using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniCar.Api.Auth;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Users;
using Xunit;

namespace UniCar.Tests.Infra;

/// <summary>
/// Sobe a API de verdade com um banco PostgreSQL temporário (criado pelas migrations e apagado no fim).
/// Usa o PostgreSQL local (usuário unicar). Para outro servidor: variável UNICAR_TEST_PG.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString =
        (Environment.GetEnvironmentVariable("UNICAR_TEST_PG") ?? "Host=localhost;Port=5432;Username=unicar;Password=unicar_dev")
        + $";Database=unicar_test_{Guid.NewGuid():N}";

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"unicar_test_{Guid.NewGuid():N}");

    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<DbContextOptions<AppDbContext>>();
            s.AddDbContext<AppDbContext>(o => o.UseNpgsql(_connectionString));
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(Emails);
            s.Configure<StorageOptions>(o => o.PrivateRoot = _storageRoot);
        });
    }

    public Task InitializeAsync()
    {
        _ = Server; // sobe a API: aplica as migrations e o seed
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        await DisposeAsync();
        if (Directory.Exists(_storageRoot)) Directory.Delete(_storageRoot, recursive: true);
    }

    /// <summary>Dá Role = Admin direto no banco (não existe rota para isso).</summary>
    public Task MakeAdminAsync(Guid userId) => WithDbAsync(async db =>
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        user.Role = Role.Admin;
        await db.SaveChangesAsync();
    });

    /// <summary>Acesso direto ao banco, para preparar cenários (ex.: aprovar motorista).</summary>
    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Faz o fluxo completo (código por e-mail + cadastro) e devolve um cliente já logado.</summary>
    public async Task<(HttpClient Client, UserDto User)> NewUserAsync(
        string? email = null, string fullName = "Ana Ribeiro", string course = "Direito")
    {
        email ??= $"t{Guid.NewGuid():N}"[..12] + "@ufpr.br";
        var client = CreateClient();

        (await client.PostAsJsonAsync("/api/auth/request-code", new { email })).EnsureSuccessStatusCode();
        var verify = await client.PostAsJsonAsync("/api/auth/verify-code", new { email, code = Emails.LastCode(email) });
        verify.EnsureSuccessStatusCode();
        var signup = (await verify.Content.ReadFromJsonAsync<VerifyCodeResponse>())!.SignupToken!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup);
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName, birthDate = "2003-05-20", cpf = TestCpf.Next(), phone = "(41) 99999-0000",
            course, period = 3, shift = "Noite"
        });
        if (!reg.IsSuccessStatusCode) throw new InvalidOperationException(await reg.Content.ReadAsStringAsync());
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth.User);
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

/// <summary>Guarda o último código mandado para cada e-mail em vez de enviar.</summary>
public partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new();

    public Task SendAsync(string to, string subject, string textBody, CancellationToken ct = default)
    {
        _codes[to] = CodeRegex().Match(subject).Value;
        return Task.CompletedTask;
    }

    public string LastCode(string email) => _codes[email];

    [GeneratedRegex(@"\d{6}")]
    private static partial Regex CodeRegex();
}

/// <summary>CNHs e placas válidas e diferentes para cada teste.</summary>
public static class TestDriver
{
    private static int _next = Random.Shared.Next(100_000_000, 900_000_000);
    private static int _plate = Random.Shared.Next(0, 17_000);

    public static string Cnh()
    {
        while (true)
        {
            var candidate = Interlocked.Increment(ref _next).ToString("D9");
            for (var dv = 0; dv < 100; dv++)
            {
                var full = candidate + dv.ToString("D2");
                if (Api.Common.Cnh.IsValid(full)) return full;
            }
        }
    }

    /// <summary>Placa Mercosul única: três letras + 1 + letra + 2 números.</summary>
    public static string Plate()
    {
        var n = Interlocked.Increment(ref _plate);
        char L(int i) => (char)('A' + i % 26);
        return $"{L(n / 676)}{L(n / 26)}{L(n)}1{L(n / 7)}{n % 100:D2}";
    }

    public static object Request(string? cnh = null, string expiry = "12/2030", string? plate = null) =>
        new { cnh = cnh ?? Cnh(), cnhExpiry = expiry, plate = plate ?? Plate(), model = "Onix 1.0", color = "Prata" };

    public static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static MultipartFormDataContent File(byte[] bytes, string name = "foto.png")
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", name);
        return form;
    }
}

/// <summary>CPFs válidos e diferentes para cada cadastro de teste.</summary>
public static class TestCpf
{
    private static int _next = Random.Shared.Next(100_000_000, 900_000_000);

    public static string Next()
    {
        var d = Interlocked.Increment(ref _next).ToString("D9").Select(c => c - '0').ToList();
        for (var len = 9; len <= 10; len++)
        {
            var sum = 0;
            for (var i = 0; i < len; i++) sum += d[i] * (len + 1 - i);
            var r = sum * 10 % 11;
            d.Add(r == 10 ? 0 : r);
        }
        return string.Concat(d);
    }
}
