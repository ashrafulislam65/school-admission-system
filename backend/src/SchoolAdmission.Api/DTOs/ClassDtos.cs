namespace SchoolAdmission.Api.DTOs;

public record CreateClassRequest(string? Name, int? TotalSeats);

public record ClassCreatedResponse(string Id, string Name, int TotalSeats);

public record ClassResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);