namespace UniCar.Api.Places;

public static class Geo
{
    /// <summary>Distância em linha reta (haversine), em km.</summary>
    public static double Km(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0;
        static double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    public static double Km(LatLng a, LatLng b) => Km(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    /// <summary>
    /// Estimativa de trajeto pelas ruas sem chamar o Google: linha reta × 1,35 a 25 km/h (trânsito de Curitiba).
    /// Usada só onde o valor é indicativo ("passa por você em ~X min") quando o Google não está configurado.
    /// </summary>
    public static RouteInfo EstimateRoute(LatLng a, LatLng b)
    {
        var km = Km(a, b) * 1.35;
        return new RouteInfo((int)Math.Round(km * 1000), (int)Math.Round(km / 25 * 3600), null);
    }
}
