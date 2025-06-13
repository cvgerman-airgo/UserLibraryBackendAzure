using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Application.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;

namespace Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Base de datos
            services.AddDbContext<ApplicationDbContext>();

            // Repositorios
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBooksRepository, BooksRepository>();

            // Servicios
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITranslationService, TranslationService>();
            services.AddScoped<IImageService, ImageService>(); // 👈 Nuevo servicio de imágenes

            // Redis u otros servicios de infraestructura (si aplica)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:Connection"];
            });

            return services;
        }
    }
}

