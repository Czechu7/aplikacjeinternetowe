using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Services.Common;

namespace API.Controllers;

public class AccountController(DataContext context, ITokenService tokenService) : BaseApiController
{
    [HttpPost("register")] // account/register
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

        using var hmac = new HMACSHA512();

        var user = new AppUser
        {
            Id = context.Users.Count() + 1,
            UserName = registerDto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key,
            Enabled2FA = registerDto.Enable2Fa,
            TwoFactorSecret = GenerateSecret() 
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        var qrCodeUrl = GenerateQrCodeUrl(user.UserName, user.TwoFactorSecret);

        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user),
            TwoFactorQrCodeUrl = qrCodeUrl 

        };
    }
    private string GenerateSecret()
    {
        var key = OtpSharp.KeyGeneration.GenerateRandomKey(20);
        return Base32Encoder.Encode(key); 
    }



    private string GenerateQrCodeUrl(string username, string secret)
    {
        var issuer = "Bai_Laby"; 
        var account = username;
        var otpUri = $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}";

        return otpUri; 
    }
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await context.Users.FirstOrDefaultAsync(x =>
            x.UserName == loginDto.Username.ToLower());

        if (user == null) return Unauthorized("Invalid username");

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
        }


        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user),
            TwoFactorRequired = user.Enabled2FA
        };
    }

    [HttpPost("verify-2fa")]
    public async Task<ActionResult<UserDto>> VerifyTwoFactorCode([FromBody] VerifyTwoFactorDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.UserName == dto.Username);
        if (user == null || !user.Enabled2FA)
            return Unauthorized("Invalid user or 2FA not enabled");


        var secretBytes = Base32Encoder.Decode(user.TwoFactorSecret);

        var totp = new OtpNet.Totp(secretBytes);
        bool isCodeValid = totp.VerifyTotp(dto.Code, out long timeStepMatched);

        if (!isCodeValid) return Unauthorized("Invalid 2FA code");

        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user),
        };
    }




    private async Task<bool> UserExists(string username) 
    {
        return await context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower()); // Bob != bob
    }
}
public class VerifyTwoFactorDto
{
    public string Username { get; set; }
    public string Code { get; set; }
}