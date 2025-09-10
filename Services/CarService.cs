using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await CarIdExistsAsync(carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            (p.EndDate == default || p.EndDate >= date)
        );
    }
    public async Task<Claim> RegisterClaimAsync(long carId, RegisterClaimDto dto, DateOnly parsedClaimDate)
    {
        var carExists = await CarIdExistsAsync(carId);
        if (!carExists)
            throw new KeyNotFoundException($"Car {carId} not found");

        var claim = new Claim
        {
            CarId = carId,
            ClaimDate = parsedClaimDate,
            Description = dto.Description,
            Amount = dto.Amount
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return claim;
    }

    public async Task<CarHistoryResponseDto> GetCarHistoryAsync(long carId)
    {
        var car = await CarIdExistsAsync(carId);
        if (!car)
            throw new KeyNotFoundException($"Car {carId} not found");

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .ToListAsync();

        var policyEvents = policies.Select(p => new CarHistoryEventDto(
            "Policy",
            p.StartDate, $"Policy with {p.Provider} ({p.StartDate:yyyy-MM-dd} to {p.EndDate:yyyy-MM-dd})"
        )).ToList();

        var claims = await _db.Claims
            .Where(c => c.CarId == carId)
            .ToListAsync();

        var claimEvents = claims.Select(c => new CarHistoryEventDto(
            "Claim",
            c.ClaimDate,
            $"{c.Description} - {c.Amount:C}"
        )).ToList();

        var combined = policyEvents.Concat(claimEvents)
            .OrderBy(e => e.Date)
            .ToList();

        return new CarHistoryResponseDto(carId, combined);
    }

    public Task<bool> CarIdExistsAsync(long carId)
    {
        return _db.Cars.AnyAsync(c => c.Id == carId);
    }
}