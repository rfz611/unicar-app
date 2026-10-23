using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Auth;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Driver;

/// <summary>Painel de análise manual dos motoristas (só Role = Admin).</summary>
[ApiController]
[Route("api/admin/drivers")]
[Authorize(Policy = AdminRequirement.Policy)]
public class AdminDriversController(AppDbContext db, SensitiveData sensitive, PrivateStorage storage, TimeProvider clock)
    : ControllerBase
{
    /// <summary>Pedidos por situação. Padrão: os que estão em análise, mais antigos primeiro.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DriverStatus status = DriverStatus.InReview, CancellationToken ct = default)
    {
        var apps = await db.DriverApplications
            .Include(a => a.User).ThenInclude(u => u!.University)
            .Where(a => a.User!.DriverStatus == status)
            .OrderBy(a => a.SubmittedAt ?? a.CreatedAt)
            .ToListAsync(ct);

        return Ok(apps.Select(a => new AdminDriverDto(
            a.UserId, a.User!.FullName, a.User.Email, a.User.University?.ShortName ?? string.Empty, a.User.DriverStatus.ToString(),
            sensitive.Decrypt(a.CnhEncrypted), a.CnhExpiresOn.ToString("MM/yyyy"), Plate.Display(a.Plate),
            a.VehicleModel, a.VehicleColor, a.CnhPhotoFile is not null, a.SelfieFile is not null, a.SubmittedAt, a.RejectionReason)));
    }

    [HttpGet("{userId:guid}/cnh-photo")]
    public Task<IActionResult> CnhPhoto(Guid userId, CancellationToken ct) => FileAsync(userId, a => a.CnhPhotoFile, ct);

    [HttpGet("{userId:guid}/selfie")]
    public Task<IActionResult> Selfie(Guid userId, CancellationToken ct) => FileAsync(userId, a => a.SelfieFile, ct);

    /// <summary>Aprova: a pessoa vira motorista e o carro do pedido passa a ser o carro dela.</summary>
    [HttpPost("{userId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid userId, CancellationToken ct)
    {
        var app = await InReviewAsync(userId, ct);
        if (app is null) return NotFound(new ApiError("not_in_review", "Não há cadastro em análise para essa pessoa."));
        if (await db.Vehicles.AnyAsync(v => v.Plate == app.Plate && v.Active && v.UserId != userId, ct))
            return Conflict(new ApiError("plate_in_use", "Essa placa já está com outro motorista."));

        foreach (var old in await db.Vehicles.Where(v => v.UserId == userId && v.Active).ToListAsync(ct))
            old.Active = false;
        db.Vehicles.Add(new Vehicle { UserId = userId, Plate = app.Plate, Model = app.VehicleModel, Color = app.VehicleColor });

        app.User!.DriverStatus = DriverStatus.Approved;
        MarkReviewed(app, null);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Recusa com o motivo (aparece para a pessoa, que pode corrigir e reenviar).</summary>
    [HttpPost("{userId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid userId, RejectRequest req, CancellationToken ct)
    {
        var reason = req.Reason?.Trim() ?? string.Empty;
        if (reason.Length is < 5 or > 300) return BadRequest(new ApiError("invalid_reason", "Explique o motivo (5 a 300 caracteres)."));

        var app = await InReviewAsync(userId, ct);
        if (app is null) return NotFound(new ApiError("not_in_review", "Não há cadastro em análise para essa pessoa."));

        app.User!.DriverStatus = DriverStatus.Rejected;
        MarkReviewed(app, reason);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private void MarkReviewed(DriverApplication app, string? reason)
    {
        app.ReviewedAt = clock.GetUtcNow();
        app.ReviewedById = User.UserId();
        app.RejectionReason = reason;
    }

    private Task<DriverApplication?> InReviewAsync(Guid userId, CancellationToken ct) =>
        db.DriverApplications.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.User!.DriverStatus == DriverStatus.InReview, ct);

    private async Task<IActionResult> FileAsync(Guid userId, Func<DriverApplication, string?> pick, CancellationToken ct)
    {
        var app = await db.DriverApplications.FirstOrDefaultAsync(a => a.UserId == userId, ct);
        var name = app is null ? null : pick(app);
        var stream = name is null ? null : storage.Open(userId, name);
        return stream is null
            ? NotFound(new ApiError("file_not_found", "Foto não enviada."))
            : File(stream, ImageUpload.ContentType(name!));
    }
}
