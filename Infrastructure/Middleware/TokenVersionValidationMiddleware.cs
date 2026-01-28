using Application.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Persistence.UnitOfWork;
using System.Security.Claims;

namespace Infrastructure.Middleware;

/// <summary>
/// Her API isteğinde access token'daki version ile cache/veritabanındaki RefreshTokenVersion'ı karşılaştırır
/// Eşleşmezse 401 Unauthorized döner
/// Performans için memory cache kullanır (5 dakika TTL)
/// </summary>
public class TokenVersionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private const int CACHE_DURATION_MINUTES = 5; // Cache süresi
    
    // Token kontrolü yapılmayacak public endpoint'ler
    private static readonly string[] _publicEndpoints = new[]
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/verify-email",
        "/api/auth/resend-verification",
        "/swagger",
        "/health"
    };

    public TokenVersionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        // 0. Public endpoint kontrolü - bu endpoint'ler için token version kontrolü yapma
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (_publicEndpoints.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }
        
        // 1. Authorization header var mı kontrol et
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            // 2. Token'dan UserId ve Version claim'lerini al
            var userIdClaim = context.User.FindFirst("UserId")?.Value;
            var versionClaim = context.User.FindFirst("TokenVersion")?.Value;

            // 3. Claim'ler varsa version kontrolü yap
            if (!string.IsNullOrEmpty(userIdClaim) && 
                !string.IsNullOrEmpty(versionClaim) &&
                int.TryParse(userIdClaim, out int userId) &&
                int.TryParse(versionClaim, out int tokenVersion))
            {
                // 4. Cache'den kullanıcının version'ını almaya çalış
                var cacheKey = $"user_token_version_{userId}";
                
                if (!cache.TryGetValue(cacheKey, out int? cachedVersion))
                {
                    // Cache'de yok, DB'den al
                    var user = await unitOfWork.Users.GetByIdAsync(userId);
                    
                    if (user != null && !user.IsDeleted && user.IsActive)
                    {
                        cachedVersion = user.RefreshTokenVersion;
                        
                        // Cache'e kaydet (5 dakika)
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES)
                        };
                        cache.Set(cacheKey, cachedVersion, cacheOptions);
                    }
                    else
                    {
                        // Kullanıcı silinmiş veya pasif
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            "{\"message\":\"Hesabınız aktif değil. Lütfen destek ekibiyle iletişime geçin.\"}");
                        return;
                    }
                }

                // 5. Version kontrolü yap
                if (cachedVersion.HasValue && cachedVersion.Value != tokenVersion)
                {
                    // Token geçersiz - oturum sonlandırılmış
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"message\":\"Oturumunuz sonlandırıldı. Lütfen tekrar giriş yapın.\"}");
                    return;
                }
            }
        }

        // 6. Kontrollerden geçtiyse bir sonraki middleware'e geç
        await _next(context);
    }
}
