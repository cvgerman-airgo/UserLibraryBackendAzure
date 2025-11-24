using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOpenLibraryService
    {
//        Task<string> GetBookDataByIsbnAsync(string isbn);
        Task<BookDto?> GetByIsbnAsync(string isbn);
        Task<List<BookDto>> SearchOpenLibraryAsync(string? title, string? author, string? language);
    }
}
