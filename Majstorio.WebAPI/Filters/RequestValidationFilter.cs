using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Majstorio.WebAPI.Filters;

public sealed class RequestValidationFilter : IAsyncActionFilter
{
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    public RequestValidationFilter(IOptions<ApiBehaviorOptions> apiBehaviorOptions)
    {
        _apiBehaviorOptions = apiBehaviorOptions.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument))
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(parameter.ParameterType);
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            if (argument == null)
            {
                context.ModelState.AddModelError(parameter.Name, "A request body is required.");
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
            return;
        }

        await next();
    }
}
