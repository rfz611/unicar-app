using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;

namespace UniCar.Api.Chat;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController(AppDbContext db, ChatService chat) : ControllerBase
{
    /// <summary>Respostas rápidas da tela do chat.</summary>
    [HttpGet("quick-replies")]
    public IActionResult QuickReplies() => Ok(ChatService.QuickReplies);

    /// <summary>Mensagens em ordem de envio. Para carregar mais antigas: ?before=(createdAt da primeira).</summary>
    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> Messages(Guid id, [FromQuery] DateTimeOffset? before, [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var participants = await chat.ParticipantsAsync(id, ct);
        if (participants is null) return NotFound(new ApiError("conversation_not_found", "Conversa não encontrada."));
        if (!participants.Contains(User.UserId()!.Value))
            return StatusCode(403, new ApiError("not_participant", "Você não está nessa viagem."));

        limit = Math.Clamp(limit, 1, 100);
        var page = await db.Messages.AsNoTracking().Include(m => m.Author)
            .Where(m => m.ConversationId == id && (before == null || m.CreatedAt < before))
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return Ok(page.OrderBy(m => m.CreatedAt).Select(MessageDto.From));
    }

    /// <summary>Enviar sem SignalR (o hub SendMessage faz o mesmo).</summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> Send(Guid id, SendMessageRequest req, CancellationToken ct)
    {
        var (message, error) = await chat.SendAsync(id, User.UserId()!.Value, req.Text, ct);
        return error?.Code switch
        {
            null => StatusCode(201, message),
            "conversation_not_found" => NotFound(error),
            "not_participant" => StatusCode(403, error),
            _ => BadRequest(error)
        };
    }
}
