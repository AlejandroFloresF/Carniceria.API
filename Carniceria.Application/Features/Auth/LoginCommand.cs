using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Carniceria.Application.Features.Auth;

public record LoginCommand(string Username, string Password) : IRequest<Result<LoginResponseDto>>;

public record LoginResponseDto(string Token, string Username, string Role);

public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public LoginHandler(IUserRepository users, IConfiguration config)
    {
        _users  = users;
        _config = config;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(cmd.Username.Trim().ToLower(), ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
            return Result.Fail<LoginResponseDto>("Usuario o contraseña incorrectos.");

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,  user.Username),
            new Claim(ClaimTypes.Role,  user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return Result.Ok(new LoginResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Username,
            user.Role.ToString()));
    }
}
