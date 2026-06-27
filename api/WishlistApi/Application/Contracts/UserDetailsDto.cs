namespace Application.Contracts;

public record UserDetailsDto(
    uint RowVersion,
    string Email,
    string? FirstName,
    string? LastName,
    string? Country,
    string? City,
    string? Address
);
