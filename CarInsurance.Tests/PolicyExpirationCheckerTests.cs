using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarInsurance.Tests {
public class PolicyExpirationCheckerTests
{
    [Fact]
    public async Task LogsExpiredPolicies_Once()
    {
         var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("carinsDb")
        .Options;

            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<PolicyExpirationChecker>();

            var now = DateTime.UtcNow;

            using (var db = new AppDbContext(options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                db.PolicyExpiration.RemoveRange(db.PolicyExpiration);
                db.Policies.RemoveRange(db.Policies);
                db.Cars.RemoveRange(db.Cars);
                db.Owners.RemoveRange(db.Owners);
                db.SaveChanges();

                db.Owners.Add(new Owner { Id = 1, Name = "Test Owner" });
                db.Cars.Add(new Car
                {
                    Id = 123,
                    Vin = "VIN123456789",
                    Make = "TestMake",
                    Model = "TestModel",
                    YearOfManufacture = 2022,
                    OwnerId = 1
                });
                db.Policies.Add(new InsurancePolicy
                {
                    Id = 1,
                    CarId = 123,
                    StartDate = DateOnly.FromDateTime(now.AddMonths(-1)),
                    EndDate = DateOnly.FromDateTime(now.AddMinutes(-30))
                });

                db.SaveChanges();
            }

            using (var db = new AppDbContext(options))
            {
                var checker = new PolicyExpirationChecker(db, logger);
                var expired = await checker.CheckAndLogExpirationsAsync(now);

                Assert.Single(expired);
                Assert.Equal(1, expired[0].Id);

                var logEntry = db.PolicyExpiration.FirstOrDefault(e => e.PolicyId == 1);
                Assert.NotNull(logEntry);
            }

            using (var db = new AppDbContext(options))
            {
                var checker = new PolicyExpirationChecker(db, logger);
                var expiredAgain = await checker.CheckAndLogExpirationsAsync(now);

                Assert.Empty(expiredAgain);
            }
        }
    }
}