namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);

public record RegisterClaimDto(string ClaimDate, string Description, decimal Amount);

public record CarHistoryEventDto(string Type, DateOnly Date, string Description);
public record CarHistoryResponseDto(long CarId, List<CarHistoryEventDto> History);