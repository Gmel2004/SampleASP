using AngleSharp;
using Dapper;
using Npgsql;
using SampleASP.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace SampleASP.Services;

public partial class ProcessingService
{
    private string _connectionString;

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public ProcessingService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<ApiResponse> ProcessRequestAsync(
        ApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiResponse();

        try
        {
            var url = DecodeUrlFromBase64(request.Url_b64, response);
            if (response.Is_error == 1) return response;

            string? pageHtml;
            try
            {
                pageHtml = DecodeBase64ToString(request.Page_b64);
            }
            catch (Exception)
            {
                return CreateErrorResponse("PAGE_DECODE_ERROR", "Error decoding page from base64");
            }

            var elementsList =
                await ParseHtmlWithAngleSharp(pageHtml, request.Selector, request.Attribute, cancellationToken);
            response.Elements_count = elementsList.Count;
            response.Elements_attr_list = elementsList.Select(t => t.AttributeValue).ToList();

            await SaveElementsToDatabase(elementsList, cancellationToken);

            var emails = EmailRegex().
                Matches(pageHtml).
                Select(m => m.Value).
                Distinct().
                ToList();

            response.Emails_count = emails.Count;
            response.Emails_list = emails;

            var decryptedText = DecryptTextWithAes256(request.Encrypted_text_bytes_b64, request.Key_bytes_b64);
            if (decryptedText == null) return CreateErrorResponse("DECRYPTION_ERROR", "Error decrypting text");
            response.Decrypted_plain_text = decryptedText;

            response.Is_error = 0;
        }
        catch (Exception ex)
        {
            return CreateErrorResponse("GENERAL_ERROR", ex.Message);
        }

        return response;
    }

    private string DecodeUrlFromBase64(string urlBase64, ApiResponse response)
    {
        try
        {
            var url = DecodeBase64ToString(urlBase64);
            response.Url = url;
            return url;
        }
        catch (Exception)
        {
            response.Is_error = 1;
            response.Error_code = "URL_DECODE_ERROR";
            response.Error_message = "Error decoding URL from base64";
            return string.Empty;
        }
    }

    private async Task<List<(string AttributeValue, string Html)>>
        ParseHtmlWithAngleSharp(string pageHtml, string selector, string attribute, CancellationToken cancellationToken)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(pageHtml), cancellationToken);
        var elements = document.
            QuerySelectorAll(selector).
            Select(t => (AttributeValue: t.GetAttribute(attribute), Html: t.OuterHtml)).
            Where(t => !string.IsNullOrEmpty(t.AttributeValue)).
            ToList();

        return elements ?? [];
    }

    private string? DecryptTextWithAes256(string encryptedTextBase64, string keyBase64)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedTextBase64);
            var keyBytes = Convert.FromBase64String(keyBase64);
            return DecryptAes256Ecb(encryptedBytes, keyBytes);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string DecodeBase64ToString(string base64String)
    {
        var bytes = Convert.FromBase64String(base64String);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string DecryptAes256Ecb(byte[] encryptedBytes, byte[] keyBytes)
    {
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes).TrimEnd('\0');
    }

    private async Task SaveElementsToDatabase(
        List<(string AttributeValue, string Html)> elements,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        string sql = @"
            INSERT INTO elements (attribute_value, html_code) 
            VALUES (@AttributeValue, @HtmlCode)";

        foreach (var (attributeValue, html) in elements)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { AttributeValue = attributeValue, HtmlCode = html },
                cancellationToken: cancellationToken));
        }
    }

    private static ApiResponse CreateErrorResponse(string errorCode, string errorMessage)
    {
        return new ApiResponse
        {
            Is_error = 1,
            Error_code = errorCode,
            Error_message = errorMessage
        };
    }
}
