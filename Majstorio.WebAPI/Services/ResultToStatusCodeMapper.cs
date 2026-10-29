using Majstorio.Model.Results;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Services;

public sealed class ResultToStatusCodeMapper
{
    public IActionResult Map(Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        var statusCode = result.ErrorType switch
        {
            ErrorType.Validation => 400,
            ErrorType.Conflict => 409,
            ErrorType.NotFound => 404,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            _ => 500
        };

        var message = statusCode == 500 ? "An unexpected error occurred." : result.Message;
        return new ObjectResult(new { message }) { StatusCode = statusCode };
    }

    public IActionResult Map<T>(Result<T> result)
        => result.IsSuccess ? new OkObjectResult(result.Value) : Map((Result)result);
}
