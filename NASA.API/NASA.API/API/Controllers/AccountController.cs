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
        // Vulnerable SQL query using string concatenation
        var rawSql = $"SELECT * FROM Users WHERE UserName = '{loginDto.Username}'";
        var user = await context.Users.FromSqlRaw(rawSql).FirstOrDefaultAsync();

        if (user == null) return Unauthorized("Invalid username");

        // Vulnerable password check
        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        var passwordMatch = true;
        for (int i = 0; i < computedHash.Length && i < user.PasswordHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i])
            {
                passwordMatch = false;
                break;
            }
        }

        if (!passwordMatch) return Unauthorized("Invalid password");

        var totp = TotpGenerator.GenerateAndStoreTotp(user.UserName);
        Console.WriteLine($"Generated TOTP for {user.UserName}: {totp}");

        return new UserDto
        {
            Id = user.Id.ToString(),
            Username = user.UserName,
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
            Id = user.Id.ToString(),
            Username = user.UserName,
            Token = tokenService.CreateToken(user)
        };
    }

    private async Task<bool> UserExists(string username)
    {
        return await context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower()); // Bob != bob
    }
}