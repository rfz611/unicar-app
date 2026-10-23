using Microsoft.EntityFrameworkCore;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Users;

/// <summary>Nota média (1 casa), quantas avaliações e quantas viagens concluídas (como motorista ou passageiro).</summary>
public record UserStats(double? Rating, int RatingsCount, int TripsCount)
{
    public static readonly UserStats Empty = new(null, 0, 0);
}

public class UserStatsService(AppDbContext db)
{
    public async Task<UserStats> ForAsync(Guid userId, CancellationToken ct = default) =>
        (await ForAsync(new[] { userId }, ct))[userId];

    /// <summary>De uma vez para vários usuários (lista de busca), em 3 consultas.</summary>
    public async Task<Dictionary<Guid, UserStats>> ForAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        var ratings = await db.Ratings.Where(r => ids.Contains(r.ToUserId))
            .GroupBy(r => r.ToUserId)
            .Select(g => new { UserId = g.Key, Avg = g.Average(x => (double)x.Stars), Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, ct);
        var asDriver = await db.Trips.Where(t => t.Status == TripStatus.Completed && ids.Contains(t.DriverId))
            .GroupBy(t => t.DriverId).Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);
        var asPassenger = await db.TripPassengers.Where(p => p.Trip!.Status == TripStatus.Completed && ids.Contains(p.PassengerId))
            .GroupBy(p => p.PassengerId).Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        return ids.ToDictionary(id => id, id =>
        {
            ratings.TryGetValue(id, out var r);
            return new UserStats(r is null ? null : Math.Round(r.Avg, 1), r?.Count ?? 0,
                asDriver.GetValueOrDefault(id) + asPassenger.GetValueOrDefault(id));
        });
    }
}
