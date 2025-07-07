using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOpenLibraryService
    {
//        Task<string> GetBookDataByIsbnAsync(string isbn);
        Task<BookDto?> GetByIsbnAsync(string isbn); // <-- Añade esta firma
    }
}
