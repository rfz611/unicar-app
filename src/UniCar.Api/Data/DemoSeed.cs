using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Common;
using UniCar.Api.Domain;
using UniCar.Api.Places;
using UniCar.Api.Pricing;

namespace UniCar.Api.Data;

public class DemoOptions
{
    public const string Section = "Demo";
    /// <summary>Só vale em Development. Liga o seed de demonstração e as rotas /api/dev.</summary>
    public bool Enabled { get; set; }
}

/// <summary>Conta da demonstração, para mostrar na tela e no log.</summary>
public record DemoAccount(string Email, string Name, string Role);

/// <summary>
/// Dados para a apresentação: universitários, motoristas aprovados, um em análise, caronas de hoje e amanhã,
/// reservas, histórico, uma sugestão de horário. Os horários são relativos ao momento do seed, por isso dá
/// para recriar antes de apresentar (POST /api/dev/demo/reset). Só mexe nas contas *.demo@.
/// </summary>
public class DemoSeed(AppDbContext db, SensitiveData sensitive, PricingService pricing, PrivateStorage storage, TimeProvider clock,
    ILogger<DemoSeed> log)
{
    public const string Marker = ".demo@";

    public static readonly DemoAccount[] Accounts =
    {
        new("ana.demo@ufpr.br", "Ana Ribeiro", "Passageira (tem reserva e histórico)"),
        new("bruno.demo@ufpr.br", "Bruno Lima", "Motorista aprovado"),
        new("carla.demo@pucpr.edu.br", "Carla Souza", "Motorista aprovada"),
        new("diego.demo@utfpr.edu.br", "Diego Martins", "Motorista aprovado (carona lotada)"),
        new("eduarda.demo@up.edu.br", "Eduarda Alves", "Motorista em análise"),
        new("felipe.demo@utfpr.edu.br", "Felipe Costa", "Passageiro"),
        new("gabriela.demo@up.edu.br", "Gabriela Rocha", "Passageira"),
        new("admin.demo@ufpr.br", "Equipe UniCar", "Admin (aprova motoristas)")
    };

    // Lugares reais de Curitiba (coordenadas aproximadas)
    private static readonly RidePlace Batel = Place("R. Bispo Dom José, 2000 - Batel, Curitiba - PR", "Batel", -25.4420, -49.2890);
    private static readonly RidePlace AguaVerde = Place("Av. República Argentina, 1500 - Água Verde, Curitiba - PR", "Água Verde", -25.4560, -49.2810);
    private static readonly RidePlace Portao = Place("Av. República Argentina, 3500 - Portão, Curitiba - PR", "Portão", -25.4740, -49.2930);
    private static readonly RidePlace Cabral = Place("R. Augusto Stresser, 800 - Cabral, Curitiba - PR", "Cabral", -25.4100, -49.2550);
    private static readonly RidePlace Politecnico = Place("Av. Cel. Francisco H. dos Santos, 100 - Jardim das Américas, Curitiba - PR", "Jardim das Américas", -25.4507, -49.2316);
    private static readonly RidePlace Pucpr = Place("R. Imaculada Conceição, 1155 - Prado Velho, Curitiba - PR", "Prado Velho", -25.4517, -49.2513);
    private static readonly RidePlace Utfpr = Place("Av. Sete de Setembro, 3165 - Rebouças, Curitiba - PR", "Rebouças", -25.4393, -49.2688);
    private static readonly RidePlace Positivo = Place("R. Prof. Pedro Viriato Parigot de Souza, 5300 - Campo Comprido, Curitiba - PR", "Campo Comprido", -25.4446, -49.3561);

    private static readonly byte[] PlaceholderPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>Cria a demonstração se ainda não existir.</summary>
    public async Task EnsureAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email.Contains(Marker), ct)) return;
        await CreateAsync(ct);
    }

    /// <summary>Apaga as contas *.demo@ (com tudo delas) e cria de novo, com horários a partir de agora.</summary>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        var ids = await db.Users.Where(u => u.Email.Contains(Marker)).Select(u => u.Id).ToListAsync(ct);
        foreach (var id in ids) storage.DeleteUserFolder(id);
        // caronas antes dos usuários: Ride → Vehicle é Restrict
        await db.Rides.Where(r => ids.Contains(r.DriverId)).ExecuteDeleteAsync(ct);
        await db.Users.Where(u => ids.Contains(u.Id)).ExecuteDeleteAsync(ct);
        await CreateAsync(ct);
    }

    private async Task CreateAsync(CancellationToken ct)
    {
        var unis = await db.Universities.Include(u => u.Domains).Include(u => u.Campuses).ToListAsync(ct);
        University Uni(string shortName) => unis.Single(u => u.ShortName == shortName);
        var now = clock.GetUtcNow();
        var cpfBase = 529_982_100;

        User NewUser(DemoAccount a, string uni, string course, int period, Shift shift, string? bio = null)
        {
            var cpf = WithCheckDigits((cpfBase++).ToString("D9"));
            var u = new User
            {
                Email = a.Email, UniversityId = Uni(uni).Id, FullName = a.Name,
                BirthDate = new DateOnly(2003, 1, 1).AddDays(cpfBase % 900), Phone = "(41) 99999-0000",
                CpfEncrypted = sensitive.Encrypt(cpf), CpfHash = sensitive.Hash(cpf),
                Course = course, Period = period, Shift = shift, Bio = bio,
                EmailVerifiedAt = now.AddDays(-20), CreatedAt = now.AddDays(-20),
                Preferences = new UserPreferences()
            };
            db.Users.Add(u);
            return u;
        }

        var ana = NewUser(Accounts[0], "UFPR", "Direito", 4, Shift.Manha, "Indo pro Politécnico quase todo dia.");
        var bruno = NewUser(Accounts[1], "UFPR", "Engenharia de Software", 7, Shift.Manha, "Saio do Batel 7h30. Pontual!");
        var carla = NewUser(Accounts[2], "PUCPR", "Medicina", 5, Shift.Manha, "Playlist boa garantida.");
        var diego = NewUser(Accounts[3], "UTFPR", "Ciência da Computação", 3, Shift.Noite);
        var eduarda = NewUser(Accounts[4], "Positivo", "Arquitetura e Urbanismo", 6, Shift.Tarde);
        var felipe = NewUser(Accounts[5], "UTFPR", "Engenharia Elétrica", 2, Shift.Manha);
        var gabriela = NewUser(Accounts[6], "Positivo", "Psicologia", 3, Shift.Manha);
        var admin = NewUser(Accounts[7], "UFPR", "Sistemas de Informação", 8, Shift.Noite);
        admin.Role = Role.Admin;
        ana.Campus = Uni("UFPR").Campuses.OrderBy(c => c.Id).First();
        db.SavedPlaces.Add(new SavedPlace
        {
            UserId = ana.Id, Kind = SavedPlaceKind.Home, Label = "Casa", Address = AguaVerde.Address,
            Neighborhood = AguaVerde.Neighborhood, Latitude = AguaVerde.Latitude, Longitude = AguaVerde.Longitude
        });

        var cnhBase = 624_729_200;
        Vehicle Approve(User driver, string plate, string model, string color)
        {
            driver.DriverStatus = DriverStatus.Approved;
            var cnh = Cnh((cnhBase++).ToString("D9"));
            db.DriverApplications.Add(new DriverApplication
            {
                UserId = driver.Id, CnhEncrypted = sensitive.Encrypt(cnh), CnhHash = sensitive.Hash("cnh:" + cnh),
                CnhExpiresOn = new DateOnly(2031, 6, 30), Plate = plate, VehicleModel = model, VehicleColor = color,
                CnhPhotoFile = "demo", SelfieFile = "demo", SubmittedAt = now.AddDays(-15), ReviewedAt = now.AddDays(-14),
                ReviewedById = admin.Id
            });
            var v = new Vehicle { UserId = driver.Id, Plate = plate, Model = model, Color = color, CreatedAt = now.AddDays(-14) };
            db.Vehicles.Add(v);
            return v;
        }

        var onix = Approve(bruno, "BRU1D23", "Chevrolet Onix", "Prata");
        var hb20 = Approve(carla, "CAR2E34", "Hyundai HB20", "Branco");
        var gol = Approve(diego, "DIE3F45", "Volkswagen Gol", "Preto");

        // Eduarda: pedido completo, esperando o admin
        eduarda.DriverStatus = DriverStatus.InReview;
        var eduardaCnh = Cnh((cnhBase++).ToString("D9"));
        db.DriverApplications.Add(new DriverApplication
        {
            UserId = eduarda.Id, CnhEncrypted = sensitive.Encrypt(eduardaCnh), CnhHash = sensitive.Hash("cnh:" + eduardaCnh),
            CnhExpiresOn = new DateOnly(2029, 3, 31), Plate = "EDU4G56", VehicleModel = "Fiat Argo", VehicleColor = "Vermelho",
            CnhPhotoFile = await storage.SaveBytesAsync(eduarda.Id, "cnh", ".png", PlaceholderPng, ct),
            SelfieFile = await storage.SaveBytesAsync(eduarda.Id, "selfie", ".png", PlaceholderPng, ct),
            SubmittedAt = now.AddHours(-3)
        });

        // Caronas: horários de Curitiba a partir de hoje
        var today = LocalTime.Today(clock);
        var tomorrow = today.AddDays(1);
        // daqui a 2h, arredondado para a próxima meia hora (ex.: 14:10 → 16:30)
        var t = LocalTime.Now(clock).AddHours(2);
        var soon = t.AddMinutes(t.Minute < 30 ? 30 - t.Minute : 60 - t.Minute)
            .AddSeconds(-t.Second).AddMilliseconds(-t.Millisecond).ToUniversalTime();

        Ride NewRide(User driver, Vehicle v, RidePlace from, RidePlace to, DateTimeOffset departure, int seats)
        {
            var route = Geo.EstimateRoute(new LatLng(from.Latitude, from.Longitude), new LatLng(to.Latitude, to.Longitude));
            var q = pricing.Quote(route.Km, route.Minutes, RideMode.Scheduled);
            var ride = new Ride
            {
                DriverId = driver.Id, VehicleId = v.Id, Vehicle = v, Origin = Copy(from), Destination = Copy(to),
                DepartureAt = departure, Seats = seats, PricePerSeat = q.Total, DriverAmountPerSeat = q.DriverAmount,
                Km = q.Km, Minutes = q.Minutes, CreatedAt = now.AddDays(-1)
            };
            db.Rides.Add(ride);
            return ride;
        }

        void Reserve(Ride ride, User passenger, DateTimeOffset? at = null)
        {
            ride.SeatsTaken++;
            db.Reservations.Add(new Reservation
            {
                RideId = ride.Id, PassengerId = passenger.Id, Price = ride.PricePerSeat, CreatedAt = at ?? now.AddHours(-5)
            });
        }

        // Hoje, daqui a ~2h: Batel → Politécnico com vagas (busca "hoje")
        NewRide(bruno, onix, Batel, Politecnico, soon, 3);
        // Amanhã cedo: Batel → Politécnico, Ana já reservou (aparece em Viagens)
        var brunoTomorrow = NewRide(bruno, onix, Batel, Politecnico, LocalTime.ToUtc(tomorrow, new TimeOnly(7, 30)), 3);
        Reserve(brunoTomorrow, ana);
        // Amanhã: Água Verde → PUCPR, 2 de 4 vagas; Felipe sugeriu 10 min depois
        var carlaTomorrow = NewRide(carla, hb20, AguaVerde, Pucpr, LocalTime.ToUtc(tomorrow, new TimeOnly(7, 45)), 4);
        Reserve(carlaTomorrow, felipe);
        Reserve(carlaTomorrow, gabriela);
        db.TimeSuggestions.Add(new TimeSuggestion
        {
            RideId = carlaTomorrow.Id, PassengerId = felipe.Id, Minutes = 10,
            DepartureAtWhenSuggested = carlaTomorrow.DepartureAt, CreatedAt = now.AddHours(-1)
        });
        // Amanhã: Portão → UTFPR lotada (2 de 2)
        var diegoFull = NewRide(diego, gol, Portao, Utfpr, LocalTime.ToUtc(tomorrow, new TimeOnly(18, 0)), 2);
        Reserve(diegoFull, felipe);
        Reserve(diegoFull, gabriela);
        // Amanhã à tarde: Prado Velho → Batel e Cabral → Politécnico (mais opções na busca)
        NewRide(carla, hb20, Pucpr, Batel, LocalTime.ToUtc(tomorrow, new TimeOnly(12, 30)), 3);
        NewRide(diego, gol, Cabral, Politecnico, LocalTime.ToUtc(tomorrow, new TimeOnly(7, 15)), 3);
        // Depois de amanhã: Batel → Positivo
        NewRide(bruno, onix, Batel, Positivo, LocalTime.ToUtc(tomorrow.AddDays(1), new TimeOnly(13, 0)), 2);

        // Histórico da Ana: duas viagens que já aconteceram
        var past1 = NewRide(diego, gol, AguaVerde, Politecnico, LocalTime.ToUtc(today.AddDays(-2), new TimeOnly(7, 20)), 3);
        Reserve(past1, ana, now.AddDays(-3));
        var past2 = NewRide(carla, hb20, Pucpr, AguaVerde, LocalTime.ToUtc(today.AddDays(-5), new TimeOnly(18, 10)), 3);
        Reserve(past2, ana, now.AddDays(-6));

        // "Me avise" da Ana para o Positivo (casa com a carona de depois de amanhã)
        db.RouteAlerts.Add(new RouteAlert
        {
            UserId = ana.Id, Label = "Universidade Positivo - Ecoville", Latitude = Positivo.Latitude, Longitude = Positivo.Longitude
        });

        await db.SaveChangesAsync(ct);
        log.LogWarning("Demonstração criada. Entre com POST /api/dev/login {{ \"email\": \"ana.demo@ufpr.br\" }}. Contas: {Accounts}",
            string.Join(", ", Accounts.Select(a => $"{a.Email} ({a.Role})")));
    }

    private static RidePlace Place(string address, string neighborhood, double lat, double lng) =>
        new() { Address = address, Neighborhood = neighborhood, Latitude = lat, Longitude = lng };

    private static RidePlace Copy(RidePlace p) => Place(p.Address, p.Neighborhood!, p.Latitude, p.Longitude);

    /// <summary>9 dígitos → CPF válido (mesmo cálculo do Cpf.IsValid).</summary>
    private static string WithCheckDigits(string nine)
    {
        var d = nine.Select(c => c - '0').ToList();
        for (var len = 9; len <= 10; len++)
        {
            var sum = 0;
            for (var i = 0; i < len; i++) sum += d[i] * (len + 1 - i);
            var r = sum * 10 % 11;
            d.Add(r == 10 ? 0 : r);
        }
        return string.Concat(d);
    }

    /// <summary>9 dígitos → CNH válida, procurando os dois verificadores.</summary>
    private static string Cnh(string nine)
    {
        for (var dv = 0; dv < 100; dv++)
        {
            var full = nine + dv.ToString("D2");
            if (Common.Cnh.IsValid(full)) return full;
        }
        throw new InvalidOperationException("CNH de demonstração inválida.");
    }
}
