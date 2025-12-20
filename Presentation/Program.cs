using Application;
using Infrastructure;
using Infrastructure.Extensions;
using Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Social Network API...");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Layer Services
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices();

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // 1. Global Exception Handler (en başta olmalı)
    app.UseGlobalExceptionHandler();

    // 2. Serilog Request Logging
    app.UseSerilogRequestLogging();

    // 3. Swagger (Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 4. HTTPS Redirection
    app.UseHttpsRedirection();

    // 5. CORS (Authentication'dan önce)
    app.UseCors("AllowAll"); // Production'da "Production" policy kullan

    // 6. Rate Limiting
    app.UseRateLimiter();

    // 7. Authentication & Authorization (sonra eklenecek)
    // app.UseAuthentication();
    app.UseAuthorization();

    // 8. Map Controllers
    app.MapControllers()
        .RequireRateLimiting("PerIpPolicy"); // Tüm controller'lara IP bazlı rate limit

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}