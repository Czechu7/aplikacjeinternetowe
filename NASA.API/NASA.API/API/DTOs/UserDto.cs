namespace API.DTOs;

public class UserDto
{
    public string? Id { get; set; }
    public required string Username { get; set; }
    public required string Token { get; set; }
    public string? TotpCode { get; set; }
}
