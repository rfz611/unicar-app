using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Auth;

/// <summary>
/// Política "Admin": token de acesso + Role = Admin no banco. Confere no banco a cada pedido
/// para que tirar o admin de alguém valha na hora, sem esperar o token vencer.
/// </summary>
public class AdminRequirement : IAuthorizationRequirement
{
    public const string Policy = "Admin";
}

public class AdminHandler(AppDbContext db) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var id = context.User.UserId();
        if (id is not null && await db.Users.AnyAsync(u => u.Id == id && u.Role == Role.Admin))
            context.Succeed(requirement);
    }
}
