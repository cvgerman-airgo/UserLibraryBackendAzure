using Domain.Entities;

namespace Application.Interfaces
{
    public interface IBooksRepository
    {
        Task<IEnumerable<Book>> GetAllAsync();
        Task<Book?> GetByIdAsync(Guid id);
        Task<Book?> GetByIsbnAsync(string isbn);
        Task<Book?> GetByIsbnAndUserIdAsync(string isbn, Guid userId);
        Task AddAsync(Book book);
        Task UpdateAsync(Book book);
        Task DeleteAsync(Book book);
//        Task CreateAsync(Book book);
        Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm);
        Task<IEnumerable<Book>> GetBooksByAuthorAsync(string author);
        Task<IEnumerable<Book>> GetBooksByGenreAsync(string genre);
        Task<IEnumerable<Book>> GetBooksByStatusAsync(ReadingStatus status);
        Task<IEnumerable<Book>> GetBooksByUserIdAsync(Guid userId);
        Task SaveChangesAsync();
        Task<Book?> GetByUserIdAndIsbnAsync(Guid userId, string isbn);
        Task<IEnumerable<Book>> GetBooksByPublisherAsync(string publisher);
    }
}
