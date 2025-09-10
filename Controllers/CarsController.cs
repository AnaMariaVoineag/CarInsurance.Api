using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return BadRequest("Invalid or impossible date format. Use YYYY-MM-DD.");

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<Claim>> RegisterClaim(long carId, [FromBody] RegisterClaimDto dto)
    {
        if (!DateOnly.TryParseExact(dto.ClaimDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedClaimDate))
            return BadRequest("Invalid or impossible claim date format. Use YYYY-MM-DD.");

        if (!await _service.CarIdExistsAsync(carId))
            return NotFound($"Car with ID {carId} not found.");

        var claim = await _service.RegisterClaimAsync(carId, dto, parsedClaimDate);
        return Ok(claim);
    }

    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<CarHistoryResponseDto>> GetCarHistory(long carId)
    {
        if (!await _service.CarIdExistsAsync(carId))
            return NotFound($"Car with ID {carId} not found.");

        var history = await _service.GetCarHistoryAsync(carId);
        return Ok(history);
    }
}