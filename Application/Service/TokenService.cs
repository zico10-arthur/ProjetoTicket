using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.Service;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GerarToken(Usuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var secretKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            throw new InvalidOperationException("A chave JWT deve ter pelo menos 32 caracteres.");

        var key = Encoding.UTF8.GetBytes(secretKey);

        var perfilId = usuario.PerfilId.ToString().ToUpper();
        var role = perfilId switch
        {
            "A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1" => "Admin",
            "B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2" => "Vendedor",
            _ => "Comprador"
        };

        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim("cpf", usuario.Cpf),
            new Claim("perfilId", usuario.PerfilId.ToString()),
            new Claim("email", usuario.Email),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        });

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}