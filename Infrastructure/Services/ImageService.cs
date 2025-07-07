using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageService> _logger;
    private readonly string _coversFolderPath;
    private readonly HttpClient _httpClient;

    public ImageService(HttpClient httpClient, IWebHostEnvironment env, ILogger<ImageService> logger)
    {
        _env = env;
        _logger = logger;
        _coversFolderPath = Path.Combine(env.WebRootPath, "covers");
        if (!Directory.Exists(_coversFolderPath))
            Directory.CreateDirectory(_coversFolderPath);

        _httpClient = httpClient;
    }

    public async Task<(string? FullCoverUrl, string? ThumbnailUrl)> DownloadAndSaveCoverAsync(string imageUrlOrBase64, string isbn)
    {
        if (string.IsNullOrWhiteSpace(imageUrlOrBase64))
        {
            _logger.LogWarning("⚠️ No se proporcionó ninguna URL o base64 para descargar.");
            return (null, null);
        }

        // 🚀 Si ya es ruta local, la devolvemos tal cual
        if (imageUrlOrBase64.StartsWith("/covers/"))
        {
            _logger.LogInformation("✅ La imagen ya está local en: {Path}", imageUrlOrBase64);
            return (imageUrlOrBase64, imageUrlOrBase64);
        }

        // 🚀 Si no es una URL absoluta válida, lo rechazamos
        if (!Uri.IsWellFormedUriString(imageUrlOrBase64, UriKind.Absolute))
        {
            _logger.LogWarning("⚠️ La URL no es absoluta ni ruta local válida: {Url}. No se descargará.", imageUrlOrBase64);
            return (null, null);
        }

//        _logger.LogInformation("⬇️ Descargando imagen desde URL: {Url}", imageUrlOrBase64);

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(imageUrlOrBase64);

            var extension = Path.GetExtension(new Uri(imageUrlOrBase64).AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var fileName = $"{isbn}{extension}";
            var filePath = Path.Combine(_coversFolderPath, fileName);
            await File.WriteAllBytesAsync(filePath, bytes);

//            _logger.LogInformation("✅ Imagen guardada en: {Path}", filePath);

            var relativePath = $"/covers/{fileName}";
            return (relativePath, relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error descargando o guardando la imagen desde {Url}", imageUrlOrBase64);
            return (null, null);
        }
    }


    public async Task<(string? FullCoverUrl, string? ThumbnailUrl)> SaveBase64ImageAsync(string base64Data, string isbn)
    {
        try
        {
            var base64Parts = base64Data.Split(',');
            if (base64Parts.Length != 2)
                return (null, null);

            var bytes = Convert.FromBase64String(base64Parts[1]);
            var ext = GetExtensionFromBase64Header(base64Parts[0]);
            var fileName = $"{isbn}{ext}";
            var relativePath = Path.Combine("covers", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine(_coversFolderPath, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllBytesAsync(absolutePath, bytes);

            // Crear miniatura
            using var image = Image.Load(bytes);
            image.Mutate(x => x.Resize(100, 150));
            var thumbFileName = $"{isbn}_thumb{ext}";
            var thumbnailRelative = Path.Combine("covers", thumbFileName).Replace("\\", "/");
            var thumbnailAbsolute = Path.Combine(_coversFolderPath, thumbFileName);
            await image.SaveAsJpegAsync(thumbnailAbsolute, new JpegEncoder { Quality = 80 });

            return ("/" + relativePath, "/" + thumbnailRelative);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando imagen desde base64");
            return (null, null);
        }
    }

    private string GetExtensionFromBase64Header(string header)
    {
        if (header.Contains("jpeg")) return ".jpg";
        if (header.Contains("png")) return ".png";
        if (header.Contains("gif")) return ".gif";
        return ".jpg";
    }
}
