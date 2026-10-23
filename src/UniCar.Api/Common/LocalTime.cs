namespace UniCar.Api.Common;

/// <summary>No banco tudo é UTC; para a pessoa, o horário é o de Curitiba (America/Sao_Paulo).</summary>
public static class LocalTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTimeOffset Now(TimeProvider clock) => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(Now(clock).DateTime);

    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    /// <summary>Data e hora de Curitiba → instante UTC para gravar.</summary>
    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
