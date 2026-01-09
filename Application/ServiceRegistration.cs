using System.Text;
using Application.Models;
using Application.Services.Abstractions;
using Application.Services.Concrete;
using Domain.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // FluentValidation - Tüm validator'ları otomatik olarak ekler
        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);

        // JWT Settings - appsettings.json'dan okur ve Options pattern ile kullanılabilir hale getirir
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings != null)
        {
            // SecretKey'i environment variable'dan al (eğer appsettings'te boşsa)
            var secretKey = string.IsNullOrEmpty(jwtSettings.SecretKey)
                ? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                : jwtSettings.SecretKey;

            // SecretKey kontrolü
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException(
                    "JWT SecretKey bulunamadı! " +
                    "Lütfen appsettings.json'da 'JwtSettings:SecretKey' ayarlayın " +
                    "veya 'JWT_SECRET_KEY' environment variable'ı tanımlayın.");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Token'ı imzalayanı doğrula
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    // Token'ı kullanacak olanı doğrula
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    // Token süresini doğrula
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // Token süresi bittiğinde hemen geçersiz olsun

                    // İmza anahtarını doğrula
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey)) // Environment variable'dan gelen key
                };

                // JWT eventi - debugging için kullanılabilir
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // Application Services - Scoped: Her HTTP request için yeni instance oluşturulur
        services.AddHttpContextAccessor(); // IHttpContextAccessor için gerekli
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IThreadService, ThreadService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IFileService, FileService>();

        return services;
    }
}
