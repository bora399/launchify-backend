using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Launchify.API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Sistemde yakalanmayan bir hata oluştu: {Message}", exception.Message);

            httpContext.Response.ContentType = "application/json";

            if (exception is ValidationException validationException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Message = "Gönderilen veriler kurallara uymuyor.",
                    Errors = validationException.Errors.Select(e => e.ErrorMessage)
                }, cancellationToken);

                return true;
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Message = "Sunucu tarafında beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            }, cancellationToken);

            return true;
        }
    }
}