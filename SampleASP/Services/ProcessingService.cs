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
        try
        {
            if (!TryDecodeBase64ToString(request.UrlB64, out var url))
                return ApiResponse.Error(ErrorCode.URL_DECODE_ERROR, "Error decoding URL from base64");

            if (!TryDecodeBase64ToString(request.PageB64, out var pageHtml))
                return ApiResponse.Error(ErrorCode.PAGE_DECODE_ERROR, "Error decoding page from base64");

            var elements = await ParseHtmlAndExtractElements(pageHtml, request.Selector, request.Attribute, cancellationToken);
            
            await SaveElementsToDatabase(elements, cancellationToken);
            
            var emails = ExtractEmails(pageHtml);

            if (!TryDecryptText(request.EncryptedTextBytesB64, request.KeyBytesB64, out var decryptedText))
                return ApiResponse.Error(ErrorCode.DECRYPTION_ERROR, "Error decrypting text");

            return new ApiResponse
            {
                IsError = 0,
                Url = url,
                ElementsCount = elements.Count,
                ElementsAttrList = elements.Select(e => e.AttributeValue).ToList(),
                EmailsCount = emails.Count,
                EmailsList = emails,
                DecryptedPlainText = decryptedText
            };
        }
        catch
        {
            // —пециально неподробна€ ошибка - чтобы не вышли лишние детали дл€ пользовател€
            // дл€ разработчиков и насто€щего production кода надо добавить логирование
            return ApiResponse.Error(ErrorCode.GENERAL_ERROR, "An unexpected error occurred");
        }
    }

    private async Task<List<HtmlElement>> ParseHtmlAndExtractElements(
        string pageHtml, 
        string selector, 
        string attribute, 
        CancellationToken cancellationToken)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(pageHtml), cancellationToken);
        
        return document
            .QuerySelectorAll(selector)
            .Select(element => new HtmlElement(
                element.GetAttribute(attribute) ?? string.Empty,
                element.OuterHtml))
            .Where(e => !string.IsNullOrEmpty(e.AttributeValue))
            .ToList();
    }

    private List<string> ExtractEmails(string pageHtml)
    {
        return EmailRegex()
            .Matches(pageHtml)
            .Select(m => m.Value)
            .ToList();
    }

    private bool TryDecryptText(string encryptedTextBase64, string keyBase64, out string decryptedText)
    {
        decryptedText = string.Empty;
        
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedTextBase64);
            var keyBytes = Convert.FromBase64String(keyBase64);
            
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            decryptedText = Encoding.UTF8.GetString(decryptedBytes).TrimEnd('\0');
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDecodeBase64ToString(string base64String, out string result)
    {
        result = string.Empty;
        
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            result = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task SaveElementsToDatabase(
        List<HtmlElement> elements,
        CancellationToken cancellationToken)
    {
        if (elements.Count == 0) return;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO elements (attribute_value, html_code) 
            VALUES (@AttributeValue, @HtmlCode)";

        var parameters = elements.Select(e => new
        {
            AttributeValue = e.AttributeValue,
            HtmlCode = e.Html
        });

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));
    }
}

public record HtmlElement(string AttributeValue, string Html);
