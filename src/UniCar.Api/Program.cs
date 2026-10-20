using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UniCar.Api.Auth;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Places;
using UniCar.Api.Pricing;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
var config = builder.Configuration;

// Banco
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("Default")));

// Configurações
builder.Services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
builder.Services.Configure<EmailCodeOptions>(config.GetSection(EmailCodeOptions.Section));
builder.Services.Configure<GoogleMapsOptions>(config.GetSection(GoogleMapsOptions.Section));
builder.Services.Configure<PricingOptions>(config.GetSection(PricingOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);

// Dados sensíveis (CPF): chaves de criptografia guardadas na pasta keys/ (fora do Git)
builder.Services.AddDataProtection()
    .SetApplicationName("UniCar")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));
builder.Services.AddSingleton<SensitiveData>();

// E-mail: sem SMTP configurado, o código aparece no console da API
var smtp = config.GetSection(SmtpOptions.Section).Get<SmtpOptions>() ?? new SmtpOptions();
if (string.IsNullOrWhiteSpace(smtp.Host))
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender>(new SmtpEmailSender(smtp));

builder.Services.AddScoped<UniversityResolver>();
builder.Services.AddScoped<EmailCodeService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<PricingService>();
builder.Services.AddHttpClient<IMapsClient, GoogleMapsClient>(c => c.Timeout = TimeSpan.FromSeconds(10));

// Login (JWT)
var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Configure Jwt:SigningKey com pelo menos 32 caracteres (veja o README).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(o =>
{
    // Padrão: só token de acesso. O token de cadastro não abre o resto da API.
    o.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireClaim(TokenPurpose.Claim, TokenPurpose.Access)
        .Build();
    o.AddPolicy(TokenPurpose.Signup, p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireClaim(TokenPurpose.Claim, TokenPurpose.Signup));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "UniCar API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    o.AddSecurityDefinition("Bearer", scheme);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Cria as tabelas e as faculdades iniciais (desenvolvimento). Em produção, usar migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.EnsureSeededAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Sem chave do Google: busca e preço respondem 503 com mensagem clara, em vez de 500.
app.Use(async (ctx, next) =>
{
    try { await next(ctx); }
    catch (MapsNotConfiguredException) when (!ctx.Response.HasStarted)
    {
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await ctx.Response.WriteAsJsonAsync(new ApiError("maps_unavailable",
            "Busca de endereço e cálculo de rota estão indisponíveis no momento."));
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

public partial class Program { }
