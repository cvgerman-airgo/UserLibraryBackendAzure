namespace Application.Interfaces
{
    public interface IImageService
    {
        Task<(string? fullPath, string? thumbnailPath)> DownloadAndSaveCoverAsync(string imageUrl, string isbn);
    }
}

