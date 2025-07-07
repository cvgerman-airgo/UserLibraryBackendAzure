namespace Application.Interfaces
{
    public interface IImageService
    {
        Task<(string? FullCoverUrl, string? ThumbnailUrl)> DownloadAndSaveCoverAsync(string imageUrlOrBase64, string isbn);
        Task<(string? FullCoverUrl, string? ThumbnailUrl)> SaveBase64ImageAsync(string base64Data, string isbn);
    }
}
