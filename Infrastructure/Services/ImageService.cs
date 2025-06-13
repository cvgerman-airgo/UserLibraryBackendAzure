using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Infrastructure.Services;
public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IWebHostEnvironment env, ILogger<ImageService> Loger)
    {
        _env = env;
        _logger = Loger;
    }

//    public Task<(string? fullPath, string? thumbnailPath)> DownloadAndSaveCoverAsync(string imageUrl, string isbn)
//    {
//        throw new NotImplementedException();
//    }

    public async Task<(string? fullPath, string? thumbnailPath)> DownloadAndSaveCoverAsync(string imageUrl, string isbn)
    {
        try
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

            // Guardar portada grande
            using var image = Image.Load(imageBytes);
            var fileName = $"{isbn}.jpg";
            var relativePath = Path.Combine("covers", fileName);
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            image.Mutate(x => x.Resize(300, 450));
            await image.SaveAsJpegAsync(absolutePath, new JpegEncoder { Quality = 90 });

            // Volver a cargar para la miniatura
            using var thumbnailImage = Image.Load(imageBytes);
            thumbnailImage.Mutate(x => x.Resize(100, 150));
            var thumbnailFileName = $"{isbn}_thumb.jpg";
            var thumbnailRelative = Path.Combine("covers", thumbnailFileName);
            var thumbnailAbsolute = Path.Combine(_env.WebRootPath, thumbnailRelative);
            await thumbnailImage.SaveAsJpegAsync(thumbnailAbsolute, new JpegEncoder { Quality = 80 });

            return ("/" + relativePath.Replace("\\", "/"), "/" + thumbnailRelative.Replace("\\", "/"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando portada o miniatura desde {imageUrl}", imageUrl);
            return (null, null);
        }
    }

}
