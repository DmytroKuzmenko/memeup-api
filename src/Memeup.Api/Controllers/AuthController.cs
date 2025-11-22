using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Memeup.Api.Domain.Auth;
using Memeup.Api.Data;

namespace Memeup.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const int RefreshReuseLimit = 10;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _config;
    private readonly MemeupDbContext _db;

    public AuthController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration config,
        MemeupDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _config = config;
        _db = db;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return Unauthorized();

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded) return Unauthorized();

        await RemoveOtherTokensAsync(user.Id);
        var refreshToken = await IssueRefreshTokenAsync(user);
        var (token, expiresAt) = await GenerateJwtTokenAsync(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(token, expiresAt, refreshToken));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "EmailAlreadyInUse" });
        }

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email : request.UserName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        const string defaultRole = "User";

        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            var roleCreateResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(defaultRole));
            if (!roleCreateResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(500, new { errors = roleCreateResult.Errors.Select(e => e.Description) });
            }
        }

        var addToRoleResult = await _userManager.AddToRoleAsync(user, defaultRole);
        if (!addToRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return StatusCode(500, new { errors = addToRoleResult.Errors.Select(e => e.Description) });
        }

        return StatusCode(201, new { user.Id, user.Email });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "RefreshTokenRequired" });
        }

        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (storedToken == null || storedToken.User == null || !storedToken.CanBeUsed(RefreshReuseLimit))
        {
            return Unauthorized();
        }

        await RemoveOtherTokensAsync(storedToken.UserId, storedToken.Id);

        storedToken.UsageCount += 1;
        if (storedToken.UsageCount >= RefreshReuseLimit)
        {
            storedToken.RevokedAt = DateTimeOffset.UtcNow;
        }

        var newRefreshToken = await IssueRefreshTokenAsync(storedToken.User);

        var (token, expiresAt) = await GenerateJwtTokenAsync(storedToken.User);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(token, expiresAt, newRefreshToken));
    }


    [HttpDelete("{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string login) //delete by username or email
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return BadRequest(new { message = "LoginRequired" });
        }

        var user = await _userManager.FindByNameAsync(login);
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(login);
        }

        if (user == null)
        {
            return NotFound();
        }

        await RemoveOtherTokensAsync(user.Id);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return StatusCode(500, new { errors = result.Errors.Select(e => e.Description) });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<(string Token, DateTime ExpiresAt)> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Name, user.UserName ?? ""),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.TryParse(_config["JWT:LifetimeMinutes"], out var m) ? m : 30);

        var token = new JwtSecurityToken(
            issuer: _config["JWT:Issuer"],
            audience: _config["JWT:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private async Task<string> IssueRefreshTokenAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(tokenBytes);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(GetRefreshLifetimeDays()),
            UsageCount = 0
        };

        await _db.RefreshTokens.AddAsync(refreshToken, ct);
        return rawToken;
    }

    private async Task RemoveOtherTokensAsync(Guid userId, Guid? keepTokenId = null, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && (!keepTokenId.HasValue || x.Id != keepTokenId.Value))
            .ToListAsync(ct);

        if (tokens.Count == 0) return;

        _db.RefreshTokens.RemoveRange(tokens);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private int GetRefreshLifetimeDays()
    {
        return int.TryParse(_config["JWT:RefreshLifetimeDays"], out var days) && days > 0
            ? days
            : 30;
    }
}

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    string? UserName);

public record RefreshTokenRequest([Required] string RefreshToken);

public record AuthResponse(string Token, DateTime ExpiresAt, string RefreshToken);
