namespace API.DTOs;


public class TotpVerificationDto
{
    public required string Username { get; set; }
    public required string TotpCode { get; set; }
}