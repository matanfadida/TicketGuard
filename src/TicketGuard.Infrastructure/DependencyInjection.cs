using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGuard.Application.Seats;
using TicketGuard.Infrastructure.Persistence;
using TicketGuard.Infrastructure.Seats;

namespace TicketGuard.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers everything Infrastructure provides. Called once from Program.cs.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TicketGuardDbContext>(options =>
        {
            // Read when the DbContext is first used, not at startup,
            // so the app (and the health check test) can start without a database.
            var connectionString = configuration.GetConnectionString("SqlServer");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'SqlServer' is missing. Set it with User Secrets (see README).");
            }

            options.UseSqlServer(connectionString);
        });

        // The current time comes from TimeProvider instead of DateTimeOffset.UtcNow,
        // so tests can control the clock (e.g. jump 10 minutes ahead to expire a hold).
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ISeatHoldService, SeatHoldService>();
        services.AddHostedService<ExpiredHoldReleaser>();

        return services;
    }
}
