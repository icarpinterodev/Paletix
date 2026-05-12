using MagatzapiV2.Data;
using MagatzapiV2.Infrastructure;
using MagatzapiV2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedContracts.Dtos;

namespace MagatzapiV2.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Usuaris> _passwordHasher = new();

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var identifier = request.Identifier.Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Unauthorized("Credencials incorrectes.");
        }

        var user = await _context.Usuaris
            .Include(item => item.IdRolNavigation)
            .Include(item => item.IdCarrecNavigation)
            .FirstOrDefaultAsync(item => item.Email == identifier || item.Dni == identifier);

        if (user is null || !VerifyPassword(user, request.Password))
        {
            return Unauthorized("Credencials incorrectes.");
        }

        if (!IsHashed(user.Password))
        {
            user.Password = _passwordHasher.HashPassword(user, request.Password);
            await _context.SaveChangesAsync();
        }

        return Ok(new LoginResponseDto
        {
            Id = user.Id,
            Nom = user.Nom,
            Cognoms = user.Cognoms,
            Email = user.Email,
            Rol = user.IdRolNavigation?.Nom ?? "",
            Carrec = user.IdCarrecNavigation?.Nom ?? "",
            SaldoPunts = user.SaldoPunts,
            Nivell = user.Nivell,
            Permissions = SimplePermissionProfile.Build(user)
        });
    }

    private bool VerifyPassword(Usuaris user, string password)
    {
        if (!IsHashed(user.Password))
        {
            return string.Equals(user.Password, password, StringComparison.Ordinal);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private static bool IsHashed(string password)
    {
        return password.StartsWith("AQAAAA", StringComparison.Ordinal);
    }
}
