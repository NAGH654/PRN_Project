using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static Services.Dtos.Responses.ValidationErrorResponse;

namespace API.Middlewares;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .Select(ms => new ValidationError
                {
                    Field = ms.Key,
                    Issues = ms.Value?.Errors.Select(e => e.ErrorMessage).ToList()
                })
                .ToArray();

            context.Result = new BadRequestObjectResult(BuildErrorResponse(errors: errors));
        }
    }
}