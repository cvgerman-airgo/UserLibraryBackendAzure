using Application.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Microsoft.OpenApi.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using StackExchange.Redis;
using Application.Mapping;
using Infrastructure;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
var isMigration = args.Contains("--is-migration");

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Console.WriteLine($"🧠 Usando cadena de conexión: {connectionString}");
// Console.WriteLine("🌍 ASPNETCORE_ENVIRONMENT: " + env.EnvironmentName);

builder.Services.AddHttpClient<IGoogleBooksService, GoogleBooksService>();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UserLibrary API", Version = "v1" });

    // 🔐 Configurar JWT para Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce 'Bearer' seguido de tu token. Ejemplo: Bearer eyJhbGciOiJIUzI1..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key no está configurada en las variables de entorno o appsettings");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// 🔐 Configurar Serilog
// Serilog es una biblioteca de registro estructurado para .NET
// que permite registrar eventos de manera eficiente y flexible.
// Se utiliza para registrar información, advertencias y errores en la aplicación.
// En este caso, se configura para leer la configuración de Serilog desde el archivo appsettings.json
// y se establece para registrar eventos en la consola.
// La configuración de Serilog se puede personalizar en el archivo appsettings.json
// para ajustar el nivel de registro, los formatos y otros destinos de registro.
// En este caso, se está utilizando Serilog para registrar eventos en la consola,
// lo que es útil para el desarrollo y la depuración de la aplicación.
// Se recomienda utilizar Serilog en lugar de la implementación predeterminada de registro de ASP.NET Core
// para obtener un mejor rendimiento y flexibilidad en el registro de eventos.

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
     .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {SourceContext} - {Message}{NewLine}")
    .CreateLogger();

builder.Host.UseSerilog(); // << IMPORTANTE



if (!isMigration)
{
    var redisConnectionString = builder.Configuration["Redis:Connection"];

    if (string.IsNullOrEmpty(redisConnectionString))
        throw new InvalidOperationException("La cadena de conexión de Redis no está configurada.");

    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddSingleton<IRedisService, RedisService>();
}

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Permitir CORS para el frontend de React
var allowedOrigins = "FrontendPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowedOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Cambia si tu frontend está en otro puerto o dominio
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAutoMapper(typeof(BookProfile).Assembly);

builder.Services.AddScoped<IBooksRepository, BooksRepository>();

builder.Services.AddHttpClient<GoogleBooksService>();

builder.Services.AddHttpClient<IOpenLibraryService, OpenLibraryService>();
builder.Services.AddHttpClient<IImageService, ImageService>();

//Con eso limpias duplicados en el log


//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();
// ✅ Middleware de desarrollo: Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilita CORS antes de autenticación/autorización
app.UseCors(allowedOrigins);

// ✅ Redirección HTTPS y uso de controladores
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
