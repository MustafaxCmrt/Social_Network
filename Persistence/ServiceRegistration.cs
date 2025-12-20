using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.UnitOfWork;

namespace Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
       var connectionString = configuration.GetConnectionString("socialnetwork");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(9, 4, 0))
            ));

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        return services;
    }
}
