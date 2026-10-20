using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do CORS (libera o Expo Web e outros clientes locais)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<UniCar.API.Services.IEmailSender, UniCar.API.Services.ConsoleEmailSender>();
builder.Services.AddControllers().AddJsonOptions(o=>o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.Configure<UniCar.API.Services.PricingOptions>(builder.Configuration.GetSection("Pricing"));
builder.Services.AddSingleton<UniCar.API.Services.PricingService>();
builder.Services.AddScoped<UniCar.API.Services.NegotiationViews>();
builder.Services.AddScoped<UniCar.API.Services.NegotiationService>();
builder.Services.AddScoped<UniCar.API.Services.ActivityService>();
builder.Services.Configure<UniCar.API.Services.Maps.MapsOptions>(builder.Configuration.GetSection("Maps"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<UniCar.API.Services.Maps.IRouteProvider,UniCar.API.Services.Maps.GoogleRouteProvider>(client=>client.Timeout=TimeSpan.FromSeconds(12));
builder.Services.AddScoped<UniCar.API.Services.Maps.RouteQuoteService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UniCar API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Insira o token JWT no campo",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var key = Encoding.ASCII.GetBytes("chave_secreta_super_segura_unicar_api_2026_jwt_token");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddHttpClient<UniCar.API.Services.Maps.GooglePlacesService>(client=>client.Timeout=TimeSpan.FromSeconds(12));
var app = builder.Build();
app.Use(async (context,next)=> {
 try { await next(context); }
 catch(UniCar.API.Services.TravelException error) { context.Response.StatusCode=error.Status; await context.Response.WriteAsJsonAsync(new { message=error.Message }); }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniCar API v1");
    });
}

// 2. Middleware do CORS (obrigatório vir ANTES de UseAuthentication e UseAuthorization)
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapControllers();

app.Run();