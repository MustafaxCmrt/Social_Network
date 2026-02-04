using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // CORS Policy
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });

            // Production için daha güvenli policy
            options.AddPolicy("Production", builder =>
            {
                builder.WithOrigins("https://yourdomain.com") // Kendi domain'ini ekle
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            // Fixed Window Rate Limiter
            options.AddFixedWindowLimiter("Fixed", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 300; // Dakikada 300 request (artırıldı: sayfa yenilemelerini desteklemek için)
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20;
            });

            // Sliding Window Rate Limiter (daha esnek)
            options.AddSlidingWindowLimiter("Sliding", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 300; // Artırıldı
                opt.SegmentsPerWindow = 6; // 10 saniyelik segmentler
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20;
            });

            // IP bazlı rate limiting (Development için gevşetildi)
            options.AddPolicy("PerIpPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 300, // IP başına dakikada 300 request (60'tan artırıldı)
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    }));

            // Global rejection behavior
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken);
            };
        });

        // Infrastructure services will be added here
        // (Email, File Storage, External APIs, etc.)

        return services;
    }
}
