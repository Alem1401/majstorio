using Microsoft.AspNetCore.Diagnostics;

namespace Majstorio.WebAPI.Handlers;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(
            new { message = "An unexpected error occurred." }, cancellationToken);
        return true;
    }
}
