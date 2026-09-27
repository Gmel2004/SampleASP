using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SampleASP.Models;
using SampleASP.Services;

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
            return Ok(ApiResponse.Error(ErrorCode.REQUEST_NULL, "Request body cannot be null or empty"));
        }

        var validationResult = _validator.Validate(request);

        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First();
            return Ok(new ApiResponse
            {
                IsError = 1,
                ErrorCode = firstError.ErrorCode,
                ErrorMessage = firstError.ErrorMessage
            });
        }

        var response = await _processingService.ProcessRequestAsync(request, cancellationToken);

        return Ok(response);
    }
}
