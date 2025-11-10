using Microsoft.Extensions.Hosting;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static Services.Dtos.Responses.ValidationErrorResponse;

namespace API.Middlewares
{
    public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                //Log original exceptions/File, Debugger, Console
                logger.LogError(ex, "An exception occurred: {ExceptionMessage}. StackTrace: {StackTrace}", 
                    ex.Message, ex.StackTrace);

                switch (ex)
                {
                    case TaskCanceledException:
                    case TimeoutException:
                        await HandleExceptionAsync(
                            context, Status408RequestTimeout, "Request time out!!!Please try again");
                        break;
                    case InvalidOperationException invalidOp:
                        // Show more details for InvalidOperationException (like 7-Zip not found, file issues)
                        var errorMsg = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                            ? invalidOp.Message
                            : "Invalid operation. Please check your input and try again.";
                        await HandleExceptionAsync(
                            context,
                            Status500InternalServerError,
                            errorMsg);
                        break;
                    default:
                        var defaultMsg = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                            ? ex.Message
                            : "Sorry, internal server error occurred. Kindly try again later";
                        await HandleExceptionAsync(
                            context,
                            Status500InternalServerError,
                            defaultMsg);
                        break;
                }
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
        {
            if (!context.Response.HasStarted)
            {
                //Display message to client
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;

                await context.Response.WriteAsync(BuildErrorResponse(message));
            }
        }
    }
}