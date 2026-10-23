using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UniCar.Api.Common;

public static class ClaimsExtensions
{
    /// <summary>Id do usuário logado (claim "sub" do token de acesso).</summary>
    public static Guid? UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
}
