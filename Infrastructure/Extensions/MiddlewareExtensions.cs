using Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}
