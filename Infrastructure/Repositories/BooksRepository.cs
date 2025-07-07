using Domain.Entities;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    public class BooksRepository : IBooksRepository
    {
        private readonly ApplicationDbContext _context;

        public BooksRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Book>> GetAllAsync()
        {
            return await _context.Books.ToListAsync();
        }

        public async Task<Book?> GetByIdAsync(Guid id)
        {
            return await _context.Books.FindAsync(id);
        }

        public async Task<Book?> GetByIsbnAsync(string isbn)
        {
            return await _context.Books
            .FirstOrDefaultAsync(b => EF.Functions.ILike(b.ISBN!, isbn));
        }

        public async Task AddAsync(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Book book)
        {
            var existing = await _context.Books.FindAsync(book.Id);
            if (existing is null)
            {
                throw new InvalidOperationException($"No se encontró el libro con Id {book.Id}");
            }

            // Actualiza todas las propiedades excepto la clave primaria
            _context.Entry(existing).CurrentValues.SetValues(book);

            await _context.SaveChangesAsync();
        }


        public async Task DeleteAsync(Book book)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(string searchTerm)
        {
            return await _context.Books
                .Where(b =>
                    EF.Functions.ILike(b.Title!, $"%{searchTerm}%") ||
                    EF.Functions.ILike(b.Author!, $"%{searchTerm}%") ||
                    EF.Functions.ILike(b.ISBN!, $"%{searchTerm}%"))
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksByAuthorAsync(string author)
        {
            return await _context.Books
                .Where(b => EF.Functions.ILike(b.Author!, author))
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksByGenreAsync(string genre)
        {
            return await _context.Books
                .Where(b => EF.Functions.ILike(b.Genre!, genre))
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksByStatusAsync(ReadingStatus status)
        {
            return await _context.Books
                .Where(b => b.Status == status)
                .ToListAsync();
        }
        public async Task<Book?> GetByIsbnAndUserIdAsync(string isbn, Guid userId)
        {
            return await _context.Books
                .FirstOrDefaultAsync(b => b.ISBN == isbn && b.UserId == userId);
        }
        public async Task<IEnumerable<Book>> GetBooksByUserIdAsync(Guid userId)
        {
            return await _context.Books
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }
    }
}
