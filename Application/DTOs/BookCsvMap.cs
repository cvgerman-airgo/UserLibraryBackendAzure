using Application.DTOs;
using CsvHelper.Configuration;

public sealed class BookCsvMap : ClassMap<BookDto>
{
    public BookCsvMap()
    {
        Map(m => m.Id).Convert(args => Guid.NewGuid());
        Map(m => m.UserId).Ignore();

        Map(m => m.Title);
        Map(m => m.Series);
        Map(m => m.ISBN);
        Map(m => m.CoverUrl);
        Map(m => m.ThumbnailUrl);
        Map(m => m.Author);
        Map(m => m.Publisher);
        Map(m => m.Genre);
        Map(m => m.Summary);

        Map(m => m.PublicationDate);
        Map(m => m.AddedDate);
        Map(m => m.StartReadingDate);
        Map(m => m.EndReadingDate);

        Map(m => m.PageCount);
        Map(m => m.Country);
        Map(m => m.Language);
        Map(m => m.Status);
        Map(m => m.LentTo);
    }
}
