namespace SchoolAdmission.Api.DTOs;

public record HoldCreatedResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt);

public record HoldResponse(
    string HoldId,
    string ClassId,
    string Status,
    DateTime ExpiresAt);