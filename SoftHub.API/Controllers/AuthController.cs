using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftHub.API.Data;
using SoftHub.API.DTOs;
using SoftHub.API.Helpers;
using SoftHub.API.Models;
using SoftHub.API.Services;

namespace SoftHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, ITokenService tokenService,
        IEmailService emailService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _config = config;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenStr = _tokenService.GenerateRefreshToken();
        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshTokenStr);

        return Ok(new AuthResponse(accessToken, refreshTokenStr, MappingHelper.ToDto(user)));
    }

    // POST /api/auth/register  (Admin only)
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "Email already in use." });

        var user = new User
        {
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMe), MappingHelper.ToDto(user));
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req)
    {
        var token = await _tokenService.GetValidRefreshTokenAsync(req.RefreshToken);
        if (token == null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var accessToken = _tokenService.GenerateAccessToken(token.User);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await _tokenService.RevokeRefreshTokenAsync(req.RefreshToken);
        await _tokenService.SaveRefreshTokenAsync(token.UserId, newRefreshToken);

        return Ok(new AuthResponse(accessToken, newRefreshToken, MappingHelper.ToDto(token.User)));
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
    {
        await _tokenService.RevokeRefreshTokenAsync(req.RefreshToken);
        return NoContent();
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return Ok(MappingHelper.ToDto(user));
    }

    // PUT /api/auth/me/password
    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        const string genericMessage = "თუ ეს ელ-ფოსტა სისტემაში არსებობს, გამოგზავნილია პაროლის აღდგენის ბმული.";

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);
        if (user == null)
            return Ok(new { message = genericMessage });

        // invalidate previous unused tokens
        var staleTokens = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        staleTokens.ForEach(t => t.IsUsed = true);

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var tokenStr = Convert.ToBase64String(rawBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Token = tokenStr,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await _db.SaveChangesAsync();

        var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendUrl}/reset-password?token={tokenStr}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);

        return Ok(new { message = genericMessage });
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == req.Token &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
            return BadRequest(new { message = "ბმული არასწორია ან ვადა გასულია." });

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        resetToken.IsUsed = true;

        await _tokenService.RevokeAllUserTokensAsync(resetToken.UserId);
        await _db.SaveChangesAsync();

        return Ok(new { message = "პაროლი წარმატებით შეიცვალა." });
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
