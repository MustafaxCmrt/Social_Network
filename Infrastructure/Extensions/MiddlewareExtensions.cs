using Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
    
    /// <summary>
    /// Token version validation middleware'ini ekler
    /// Her API isteğinde access token'daki version ile DB'deki RefreshTokenVersion'ı kontrol eder
    /// </summary>
    public static IApplicationBuilder UseTokenVersionValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TokenVersionValidationMiddleware>();
    }
}
