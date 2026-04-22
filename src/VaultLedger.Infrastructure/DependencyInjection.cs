using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaultLedger.Application.Common.Interfaces;
using VaultLedger.Domain.Interfaces;
using VaultLedger.Infrastructure.Persistence;
using VaultLedger.Infrastructure.Services;

namespace VaultLedger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Expose AppDbContext via its interface so handlers depend only on IAppDbContext.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // One TenantContext instance per request, shared between ITenantContext consumers
        // and middleware that casts to TenantContext to call SetContext.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        return services;
    }
}
