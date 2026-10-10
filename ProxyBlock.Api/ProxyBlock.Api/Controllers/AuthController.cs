using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProxyBlock.Api.Data;
using ProxyBlock.Api.DTOs;
using ProxyBlock.Api.Models;
using ProxyBlock.Api.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace ProxyBlock.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly ILogger<AuthController> _log;
    public AuthController(AppDbContext db, EmailService email, ILogger<AuthController> log)
    {
        _db = db;
        _email = email;
        _log = log;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) ||
            !req.Email.EndsWith(".edu.pk", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Please use your university email ending with .edu.pk.");

        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest("Email already registered.");

        if (req.Role == UserRole.Student)
        {
            var roll = req.RollNumber?.Trim().ToUpperInvariant() ?? "";
            if (!Regex.IsMatch(roll, @"^[FS]\d{2}[A-Z]+\d[A-Z]\d+$"))
                return BadRequest("Roll number must follow the university pattern (e.g. F25BARIN1M01379).");

            if (await _db.Users.AnyAsync(u => u.RollNumber == req.RollNumber))
                return BadRequest("Roll number already registered.");
        }

        var user = new User
        {
            FullName = req.FullName,
            RollNumber = req.RollNumber,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Gender = req.Gender,
            Role = req.Role,
            IsEmailVerified = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var code = Random.Shared.Next(100000, 1000000).ToString();
        _db.EmailOtps.Add(new EmailOtp
        {
            Email = user.Email,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await _db.SaveChangesAsync();

        // Send in the background so a slow mail server never blocks registration.
        // If it fails, the student taps Resend on the verify screen.
        _ = Task.Run(async () =>
        {
            try { await _email.SendOtpAsync(user.Email, code); }
            catch (Exception ex) { _log.LogError(ex, "OTP email failed to send"); }
        });

        return Ok(new
        {
            user.Id,
            user.Email,
            emailSent = true,
            message = "Verification code sent to your university email."
        });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest req)
    {
        var otp = await _db.EmailOtps
            .Where(o => o.Email == req.Email && !o.IsUsed)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        if (otp == null || otp.Code != req.Code.Trim() || DateTime.UtcNow > otp.ExpiresAt)
            return BadRequest("Invalid or expired code.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null) return BadRequest("Account not found.");

        otp.IsUsed = true;
        user.IsEmailVerified = true;
        await _db.SaveChangesAsync();
        return Ok(new { verified = true });
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp(ResendOtpRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null) return BadRequest("Account not found.");
        if (user.IsEmailVerified) return BadRequest("Email already verified.");

        var code = Random.Shared.Next(100000, 1000000).ToString();
        _db.EmailOtps.Add(new EmailOtp
        {
            Email = user.Email,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await _db.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try { await _email.SendOtpAsync(user.Email, code); }
            catch (Exception ex) { _log.LogError(ex, "OTP resend failed"); }
        });

        return Ok(new { sent = true });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        if (!user.IsEmailVerified)
            return Unauthorized("Please verify your email first. Check your inbox for the code.");

        return Ok(new { token = GenerateToken(user), user.Id, user.FullName, user.Role });
    }

    private string GenerateToken(User user)
    {
        var jwt = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"], audience: jwt["Audience"], claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"]!)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
