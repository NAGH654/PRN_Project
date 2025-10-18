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
                logger.LogError("An exception occurred: {ExceptionMessage}", ex.Message);

                switch (ex)
                {
                    case TaskCanceledException:
                    case TimeoutException:
                        await HandleExceptionAsync(
                            context, Status408RequestTimeout, "Request time out!!!Please try again");
                        break;
                    default:
                        await HandleExceptionAsync(
                            context,
                            Status500InternalServerError,
                            "Sorry, internal server error occurred. Kindly try again later");
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