using Application.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services.Concrete;

/// <summary>
/// Token version cache yönetimi için servis implementasyonu
/// </summary>
public class TokenCacheService : ITokenCacheService
{
    private readonly IMemoryCache _cache;

    public TokenCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Kullanıcının token version cache'ini temizler
    /// RefreshTokenVersion değiştiğinde bu metod çağrılmalı
    /// </summary>
    public void InvalidateUserTokenCache(int userId)
    {
        var cacheKey = $"user_token_version_{userId}";
        _cache.Remove(cacheKey);
    }
}
