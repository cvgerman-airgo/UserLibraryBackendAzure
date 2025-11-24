
namespace Application.DTOs
{
    public class GoogleBooksApiResponse
    {
        public List<GoogleBookItem>? Items { get; set; }
    }

    public class GoogleBookItem
    {
        public GoogleVolumeInfo? VolumeInfo { get; set; }
        public GoogleAccessInfo? AccessInfo { get; set; }
    }

    public class GoogleVolumeInfo
    {
        public string? Title { get; set; }
        public List<string>? Authors { get; set; }
        public string? Publisher { get; set; }
        public string? Description { get; set; }
        public List<string>? Categories { get; set; }
        public int? PageCount { get; set; }
        public string? PublishedDate { get; set; }
        public string? Language { get; set; }
        public GoogleImageLinks? ImageLinks { get; set; }
        public List<GoogleIndustryIdentifier>? IndustryIdentifiers { get; set; }
    }

    public class GoogleImageLinks
    {
        public string? Thumbnail { get; set; }
        public string? SmallThumbnail { get; set; }
    }

    public class GoogleAccessInfo
    {
        public string? Country { get; set; }
    }
    public class GoogleIndustryIdentifier
    {
        public string? Type { get; set; }  // "ISBN_10", "ISBN_13"
        public string? Identifier { get; set; }
    }

}
