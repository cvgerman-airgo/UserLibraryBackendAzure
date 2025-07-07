using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IGoogleBooksService
    {
        Task<BookDto?> SearchByIsbnAsync(string isbn);
        Task<List<BookDto>> SearchGoogleBooksAsync(string? title, string? author, string? language);
    }
}
