using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Services;

namespace API.Controllers;

public class AccountController(DataContext context, ITokenService tokenService) : BaseApiController
{
    [HttpPost("register")]
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
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Generate and display TOTP for testing

        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user)
        };
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

        // Generate TOTP after successful password verification
        var totp = TotpGenerator.GenerateAndStoreTotp(user.UserName);
        Console.WriteLine($"Generated TOTP for {user.UserName}: {totp}");

        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user),
            TotpCode = totp
        };
    }

    [HttpPost("verify-totp")]
    public async Task<ActionResult<UserDto>> VerifyTotp(TotpVerificationDto totpDto)
    {
        var user = await context.Users.FirstOrDefaultAsync(x =>
            x.UserName == totpDto.Username.ToLower());

        if (user == null) return Unauthorized("Invalid username");

        if (!TotpGenerator.ValidateTotp(user.UserName, totpDto.TotpCode))
        {
            return Unauthorized("Invalid or expired TOTP code");
        }

        return new UserDto
        {
            Username = user.UserName,
            Token = tokenService.CreateToken(user)
        };
    }

    private async Task<bool> UserExists(string username)
    {
        return await context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower()); // Bob != bob
    }
}