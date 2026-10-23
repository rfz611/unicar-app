using UniCar.Api.Places;

namespace UniCar.Api.Trips;

/// <summary>
/// Quem recebe um "Pedir agora": motorista dirigindo cujo desvio para buscar e deixar o passageiro é de
/// até 2 km e 5 min. Desvio = (motorista→embarque + embarque→destino + destino→destino do motorista)
/// − (motorista→destino do motorista). Estimativa por linha reta (rápida, sem custo de Google por motorista).
/// </summary>
public static class RideMatching
{
    public const double MaxDetourKm = 2;
    public const int MaxDetourMinutes = 5;
    /// <summary>Não oferece para quem está longe demais do embarque, mesmo indo na mesma direção.</summary>
    public const double MaxPickupKm = 5;

    public record Detour(double PickupKm, double ExtraKm, int ExtraMinutes)
    {
        public bool Fits => PickupKm <= MaxPickupKm && ExtraKm <= MaxDetourKm && ExtraMinutes <= MaxDetourMinutes;
    }

    public static Detour Compute(LatLng driver, LatLng driverDestination, LatLng pickup, LatLng dropoff)
    {
        static double Road(LatLng a, LatLng b) => Geo.EstimateRoute(a, b).Km;
        var pickupKm = Road(driver, pickup);
        var withPassenger = pickupKm + Road(pickup, dropoff) + Road(dropoff, driverDestination);
        var alone = Road(driver, driverDestination);
        var extraKm = Math.Max(0, withPassenger - alone);
        var extraMinutes = (int)Math.Ceiling(extraKm / 25 * 60); // mesma velocidade do Geo.EstimateRoute
        return new Detour(Math.Round(pickupKm, 1), Math.Round(extraKm, 1), extraMinutes);
    }
}
