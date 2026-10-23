using System.Collections.Concurrent;

namespace UniCar.Api.Realtime;

/// <summary>Motorista "dirigindo": onde está e para onde vai.</summary>
public record DriverPosition(Guid DriverId, double Latitude, double Longitude,
    double DestinationLatitude, double DestinationLongitude, DateTimeOffset UpdatedAt);

/// <summary>
/// Quem está dirigindo agora, em memória (some se a API reiniciar, e tudo bem: o app manda de novo).
/// Sem notícia há mais de 2 minutos, o motorista deixa de receber pedidos.
/// Também lembra para quem cada pedido foi oferecido, para avisar quando ele fechar.
/// </summary>
public class DriverPresence(TimeProvider clock)
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    private readonly ConcurrentDictionary<Guid, DriverPosition> _online = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, byte>> _offers = new();

    public DriverPosition SetOnline(Guid driverId, double lat, double lng, double destLat, double destLng)
    {
        var p = new DriverPosition(driverId, lat, lng, destLat, destLng, clock.GetUtcNow());
        _online[driverId] = p;
        return p;
    }

    public void SetOffline(Guid driverId) => _online.TryRemove(driverId, out _);

    /// <summary>Atualiza a posição de quem já está online (mantém o destino).</summary>
    public void UpdatePosition(Guid driverId, double lat, double lng)
    {
        if (_online.TryGetValue(driverId, out var p))
            _online[driverId] = p with { Latitude = lat, Longitude = lng, UpdatedAt = clock.GetUtcNow() };
    }

    public DriverPosition? Get(Guid driverId) =>
        _online.TryGetValue(driverId, out var p) && clock.GetUtcNow() - p.UpdatedAt <= Ttl ? p : null;

    public IReadOnlyList<DriverPosition> Online()
    {
        var now = clock.GetUtcNow();
        return _online.Values.Where(p => now - p.UpdatedAt <= Ttl).ToList();
    }

    public void RecordOffer(Guid requestId, Guid driverId) =>
        _offers.GetOrAdd(requestId, _ => new ConcurrentDictionary<Guid, byte>())[driverId] = 0;

    /// <summary>Motoristas que receberam o pedido (e esquece a lista).</summary>
    public IReadOnlyCollection<Guid> TakeOffers(Guid requestId) =>
        _offers.TryRemove(requestId, out var set) ? set.Keys.ToList() : Array.Empty<Guid>();
}
