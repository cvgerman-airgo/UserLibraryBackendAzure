using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Repositories; // Ajusta según tu proyecto
using Domain.Entities;
using Application.DTOs;
using Application.Interfaces;

namespace UserLibraryBackEndApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ToolsController : ControllerBase
    {
        private readonly IBooksRepository _bookRepository;

        public ToolsController(IBooksRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        [HttpPost("merge-publisher")]
        [Authorize]
        public async Task<IActionResult> MergePublisher([FromBody] MergeRequest request)
        {
            if (string.IsNullOrEmpty(request.OldValue) || string.IsNullOrEmpty(request.NewValue))
                return BadRequest("OldValue y NewValue son obligatorios");

            // Buscar libros con la editorial OldValue
            var books = await _bookRepository.GetBooksByPublisherAsync(request.OldValue);

            foreach (var book in books)
            {
                book.Publisher = request.NewValue; // Reemplazar
                await _bookRepository.UpdateAsync(book);
            }

            return Ok(new { message = $"Editorial '{request.OldValue}' fusionada en '{request.NewValue}'" });
        }

        [HttpPost("merge-author")]
        [Authorize]
        public async Task<IActionResult> MergeAuthor([FromBody] MergeRequest request)
        {
            if (string.IsNullOrEmpty(request.OldValue) || string.IsNullOrEmpty(request.NewValue))
                return BadRequest("OldValue y NewValue son obligatorios");

            // Buscar libros con el autor OldValue
            var books = await _bookRepository.GetBooksByAuthorAsync(request.OldValue);

            foreach (var book in books)
            {
                book.Author = request.NewValue; // Reemplazar
                await _bookRepository.UpdateAsync(book);
            }

            return Ok(new { message = $"Autor '{request.OldValue}' fusionado en '{request.NewValue}'" });
        }
    }
}
