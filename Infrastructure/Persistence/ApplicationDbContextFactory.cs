using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using Infrastructure.Persistence;

namespace UserLibraryBackEndApi.Infrastructure;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Permite pasar el entorno como argumento, por ejemplo: "Docker"
        var environment = args.FirstOrDefault()?.TrimStart('-') ?? "Development";
        var configFile = $"appsettings.{environment}.json";

        Console.WriteLine($"🧠 Usando archivo de configuración: {configFile}");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFile, optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"🧠 Cadena de conexión: {connectionString}");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}


