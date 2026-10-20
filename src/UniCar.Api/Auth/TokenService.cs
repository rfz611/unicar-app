using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UniCar.Api.Domain;

namespace UniCar.Api.Auth;

public class TokenService(IOptions<JwtOptions> opt, TimeProvider clock)
{
    private readonly JwtOptions _o = opt.Value;

    /// <summary>Token de acesso normal, depois que a pessoa tem conta.</summary>
    public string CreateAccessToken(User user) => Create(new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(TokenPurpose.Claim, TokenPurpose.Access)
    }, clock.GetUtcNow().AddDays(_o.AccessTokenDays));

    /// <summary>Token curto que só serve para terminar o cadastro, depois que o e-mail foi confirmado.</summary>
    public string CreateSignupToken(string email) => Create(new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, email),
        new Claim(JwtRegisteredClaimNames.Email, email),
        new Claim(TokenPurpose.Claim, TokenPurpose.Signup)
    }, clock.GetUtcNow().AddMinutes(_o.SignupTokenMinutes));

    private string Create(IEnumerable<Claim> claims, DateTimeOffset expires)
    {
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, clock.GetUtcNow().UtcDateTime, expires.UtcDateTime, creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
