using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SampleASP.Models;
using SampleASP.Services;
using System.Text.Json;

namespace SampleASP.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class ApiController : ControllerBase
{
    private readonly ProcessingService _processingService;
    private readonly IValidator<ApiRequest> _validator;

    public ApiController(ProcessingService processingService, IValidator<ApiRequest> validator)
    {
        _processingService = processingService;
        _validator = validator;
    }

    /// <summary>
    /// Обработка HTML данных: парсинг, поиск элементов, расшифровка
    /// </summary>
    /// <param name="request">Входящие данные</param>
    /// <returns>Результат обработки в формате JSON</returns>
    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ApiRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            var nullResponse = new ApiResponse
            {
                Is_error = 1,
                Error_code = "REQUEST_NULL",
                Error_message = "Request body cannot be null or empty"
            };
            return Content(SerializeWithIndentation(nullResponse), "application/json");
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First();
            var errorResponse = new ApiResponse
            {
                Is_error = 1,
                Error_code = firstError.ErrorCode,
                Error_message = firstError.ErrorMessage
            };

            return Content(SerializeWithIndentation(errorResponse), "application/json");
        }

        var response = await _processingService.ProcessRequestAsync(request, cancellationToken);

        return Content(SerializeWithIndentation(response), "application/json");
    }

    private static string SerializeWithIndentation(ApiResponse response)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(response, options);
    }
}
