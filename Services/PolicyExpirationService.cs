using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class PolicyExpirationService(IServiceScopeFactory scopeFactory, ILogger<PolicyExpirationService> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<PolicyExpirationService> _logger = logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAndLogExpirations(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
        private async Task CheckAndLogExpirations(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);

            var recentExpirations = await db.Policies
                .Where(p =>
                    p.EndDate <= DateOnly.FromDateTime(now) &&
                    p.EndDate >= DateOnly.FromDateTime(oneHourAgo) &&
                    !db.PolicyExpiration.Any(e => e.PolicyId == p.Id))
                .ToListAsync(token);

            foreach (var policy in recentExpirations)
            {
                _logger.LogInformation("Policy ID {PolicyId} for Car ID {CarId} expired on {EndDate}.", policy.Id, policy.CarId, policy.EndDate);

                db.PolicyExpiration.Add(new PolicyExpiration
                {
                    PolicyId = policy.Id,
                    LoggedAt = now
                });
            }

            if (recentExpirations.Count != 0)
            {
                await db.SaveChangesAsync(token);
            }
        }
    }
}
