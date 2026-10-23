using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Driver;

/// <summary>"Virar motorista": dados da CNH e do carro, foto da CNH e selfie. Depois, análise manual no painel.</summary>
[ApiController]
[Route("api/driver")]
[Authorize]
public class DriverController(AppDbContext db, SensitiveData sensitive, PrivateStorage storage, TimeProvider clock) : ControllerBase
{
    [HttpGet("application")]
    public async Task<IActionResult> GetApplication(CancellationToken ct)
    {
        var (user, app) = await LoadAsync(ct);
        if (user is null) return Unauthorized();
        return app is null
            ? NotFound(new ApiError("no_application", "Você ainda não pediu para ser motorista."))
            : Ok(Dto(app, user));
    }

    /// <summary>Envia ou corrige os dados. Se as duas fotos já estiverem lá, vai direto para análise.</summary>
    [HttpPost("application")]
    public async Task<IActionResult> Submit(DriverApplicationRequest req, CancellationToken ct)
    {
        var (user, app) = await LoadAsync(ct);
        if (user is null) return Unauthorized();
        if (user.DriverStatus == DriverStatus.Approved)
            return Conflict(new ApiError("already_driver", "Você já é motorista verificado."));
        if (user.DriverStatus == DriverStatus.InReview)
            return Conflict(new ApiError("in_review", "Seu cadastro já está em análise. Aguarde a resposta."));

        var error = Validate(req, LocalTime.Today(clock), out var cnh, out var expiresOn, out var plate);
        if (error is not null) return BadRequest(error);

        var cnhHash = sensitive.Hash("cnh:" + cnh);
        if (await db.DriverApplications.AnyAsync(a => a.CnhHash == cnhHash && a.UserId != user.Id, ct))
            return Conflict(new ApiError("cnh_in_use", "Essa CNH já está em outra conta."));
        if (await db.Vehicles.AnyAsync(v => v.Plate == plate && v.Active && v.UserId != user.Id, ct))
            return Conflict(new ApiError("plate_in_use", "Esse carro já está cadastrado por outro motorista."));

        if (app is null)
        {
            app = new DriverApplication
            {
                UserId = user.Id, CnhEncrypted = "", CnhHash = "", Plate = "", VehicleModel = "", VehicleColor = ""
            };
            db.DriverApplications.Add(app);
        }
        app.CnhEncrypted = sensitive.Encrypt(cnh);
        app.CnhHash = cnhHash;
        app.CnhExpiresOn = expiresOn;
        app.Plate = plate;
        app.VehicleModel = req.Model.Trim();
        app.VehicleColor = req.Color.Trim();
        SendToReviewIfComplete(user, app);

        await db.SaveChangesAsync(ct);
        return Ok(Dto(app, user));
    }

    [HttpPost("application/cnh-photo")]
    [RequestSizeLimit(ImageUpload.MaxBytes + 64 * 1024)]
    public Task<IActionResult> UploadCnhPhoto(IFormFile? file, CancellationToken ct) => UploadAsync(file, isSelfie: false, ct);

    [HttpPost("application/selfie")]
    [RequestSizeLimit(ImageUpload.MaxBytes + 64 * 1024)]
    public Task<IActionResult> UploadSelfie(IFormFile? file, CancellationToken ct) => UploadAsync(file, isSelfie: true, ct);

    /// <summary>Carro do motorista aprovado (aparece nas caronas que ele oferece).</summary>
    [HttpGet("vehicle")]
    public async Task<IActionResult> GetVehicle(CancellationToken ct)
    {
        var id = User.UserId();
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.UserId == id && v.Active, ct);
        return vehicle is null
            ? NotFound(new ApiError("no_vehicle", "Você ainda não é motorista verificado."))
            : Ok(VehicleDto.From(vehicle));
    }

    private async Task<IActionResult> UploadAsync(IFormFile? file, bool isSelfie, CancellationToken ct)
    {
        var (user, app) = await LoadAsync(ct);
        if (user is null) return Unauthorized();
        if (app is null) return BadRequest(new ApiError("no_application", "Envie primeiro os dados da CNH e do carro."));
        if (user.DriverStatus is DriverStatus.Approved or DriverStatus.InReview)
            return Conflict(new ApiError("locked", "Não dá para trocar as fotos agora."));

        var error = ImageUpload.Check(file, out var extension);
        if (error is not null) return BadRequest(error);

        if (isSelfie)
            app.SelfieFile = await storage.SaveAsync(user.Id, "selfie", extension, file!, app.SelfieFile, ct);
        else
            app.CnhPhotoFile = await storage.SaveAsync(user.Id, "cnh", extension, file!, app.CnhPhotoFile, ct);
        SendToReviewIfComplete(user, app);

        await db.SaveChangesAsync(ct);
        return Ok(Dto(app, user));
    }

    private void SendToReviewIfComplete(User user, DriverApplication app)
    {
        if (!app.IsComplete) return;
        user.DriverStatus = DriverStatus.InReview;
        app.SubmittedAt = clock.GetUtcNow();
        // nova análise: a recusa anterior deixa de valer
        app.RejectionReason = null;
        app.ReviewedAt = null;
        app.ReviewedById = null;
    }

    public static ApiError? Validate(DriverApplicationRequest req, DateOnly today,
        out string cnh, out DateOnly expiresOn, out string plate)
    {
        cnh = Cpf.Digits(req.Cnh);
        plate = Common.Plate.Normalize(req.Plate);
        expiresOn = default;

        if (!Cnh.IsValid(cnh)) return new ApiError("invalid_cnh", "Número da CNH inválido. São 11 números, confira.");
        if (!Cnh.TryParseExpiry(req.CnhExpiry, out expiresOn))
            return new ApiError("invalid_cnh_expiry", "Validade no formato MM/AAAA.");
        if (expiresOn < today) return new ApiError("cnh_expired", "Sua CNH está vencida.");
        if (!Common.Plate.IsValid(plate)) return new ApiError("invalid_plate", "Placa inválida. Ex.: ABC1234 ou ABC1D23.");
        var model = req.Model?.Trim() ?? string.Empty;
        if (model.Length is < 2 or > 60) return new ApiError("invalid_model", "Escreva o modelo do carro.");
        var color = req.Color?.Trim() ?? string.Empty;
        if (color.Length is < 3 or > 30) return new ApiError("invalid_color", "Escreva a cor do carro.");
        return null;
    }

    private async Task<(User? User, DriverApplication? App)> LoadAsync(CancellationToken ct)
    {
        var id = User.UserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return (null, null);
        return (user, await db.DriverApplications.FirstOrDefaultAsync(a => a.UserId == id, ct));
    }

    private DriverApplicationDto Dto(DriverApplication app, User user) =>
        DriverApplicationDto.From(app, user.DriverStatus, sensitive.Decrypt(app.CnhEncrypted));
}
