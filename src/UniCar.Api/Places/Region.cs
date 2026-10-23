namespace UniCar.Api.Places;

public static class Region
{
    /// <summary>O ponto está dentro do retângulo da região metropolitana (o mesmo da busca do Google)?</summary>
    public static bool Contains(GoogleMapsOptions o, double lat, double lng) =>
        lat >= o.MinLatitude && lat <= o.MaxLatitude && lng >= o.MinLongitude && lng <= o.MaxLongitude;
}
