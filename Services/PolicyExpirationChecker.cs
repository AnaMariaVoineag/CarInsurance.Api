using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class PolicyExpirationChecker(AppDbContext db, ILogger<PolicyExpirationChecker> logger)
    {
        private readonly AppDbContext _db = db;
        private readonly ILogger<PolicyExpirationChecker> _logger = logger;

        public async Task<List<InsurancePolicy>> CheckAndLogExpirationsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var oneHourAgo = nowUtc.AddHours(-1);

            var expiredPolicies = await _db.Policies
                .Where(p =>
                    p.EndDate <= DateOnly.FromDateTime(nowUtc) &&
                    p.EndDate >= DateOnly.FromDateTime(oneHourAgo) &&
                    !_db.PolicyExpiration.Any(e => e.PolicyId == p.Id))
                .ToListAsync(cancellationToken);

            foreach (var policy in expiredPolicies)
            {
                _logger.LogInformation("Policy ID {PolicyId} for Car ID {CarId} expired on {EndDate}.",
                    policy.Id, policy.CarId, policy.EndDate);

                _db.PolicyExpiration.Add(new PolicyExpiration
                {
                    PolicyId = policy.Id,
                    LoggedAt = nowUtc
                });
            }

            if (expiredPolicies.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            return expiredPolicies;
        }
    }
}
