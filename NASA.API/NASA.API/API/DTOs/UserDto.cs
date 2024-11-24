namespace API.DTOs;

public class UserDto
{
    public required string Username { get; set; }
    public required string Token { get; set; }

    public bool TwoFactorRequired { get; set; }
    public string TwoFactorQrCodeUrl { get; set; }
}
