using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Application.Interfaces;
using Application.Helpers;
using Infrastructure.Estensions;

namespace UserLibraryBackEndApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBooksRepository _bookRepository;
        private readonly IMapper _mapper;
        private readonly GoogleBooksService _googleBooksService;
        private readonly ILogger<BooksController> _logger;
        private readonly IImageService _imageService;
        private readonly IOpenLibraryService _openLibraryService;

        public BooksController(
            IBooksRepository bookRepository,
            IMapper mapper,
            GoogleBooksService googleBooksService,
            ILogger<BooksController> logger,
            IImageService imageService,
            IOpenLibraryService openLibraryService)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
            _googleBooksService = googleBooksService;
            _logger = logger;
            _imageService = imageService;
            _openLibraryService = openLibraryService;
        }

        // GET: api/books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAll()
        {
            var books = await _bookRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }

        // GET: api/books/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BookDto>> GetById(Guid id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return NotFound();
            return Ok(_mapper.Map<BookDto>(book));
        }

        // GET: api/books/isbn/{isbn}
        [HttpGet("isbn/{isbn}")]
        public async Task<ActionResult<BookDto>> GetByIsbn(string isbn)
        {
            var book = await _bookRepository.GetByIsbnAsync(isbn);
            if (book == null) return NotFound();
            return Ok(_mapper.Map<BookDto>(book));
        }

        // GET: api/books/search?term=palabra
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BookDto>>> Search([FromQuery] string term)
        {
            var books = await _bookRepository.SearchBooksAsync(term);
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }

        // GET: api/books/author/{author}
        [HttpGet("author/{author}")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetByAuthor(string author)
        {
            var books = await _bookRepository.GetBooksByAuthorAsync(author);
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }

        // GET: api/books/genre/{genre}
        [HttpGet("genre/{genre}")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetByGenre(string genre)
        {
            var books = await _bookRepository.GetBooksByGenreAsync(genre);
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }

        // GET: api/books/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetByStatus(ReadingStatus status)
        {
            var books = await _bookRepository.GetBooksByStatusAsync(status);
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }

        // POST: api/books
        [HttpPost]
        public async Task<ActionResult<BookDto>> Create([FromBody] CreateBookRequest request)
        {
            var book = _mapper.Map<Book>(request);
            book.Id = Guid.NewGuid();
            await _bookRepository.AddAsync(book);
            var bookDto = _mapper.Map<BookDto>(book);
            return CreatedAtAction(nameof(GetById), new { id = book.Id }, bookDto);
        }


        // PUT: api/books/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookRequest request)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return NotFound();

            _mapper.Map(request, book);

            // Convierte fechas a UTC si tienen valor y Kind no es UTC
            if (book.StartReadingDate.HasValue)
                book.StartReadingDate = DateTime.SpecifyKind(book.StartReadingDate.Value, DateTimeKind.Utc);

            if (book.EndReadingDate.HasValue)
                book.EndReadingDate = DateTime.SpecifyKind(book.EndReadingDate.Value, DateTimeKind.Utc);

            if (book.PublicationDate.HasValue)
                book.PublicationDate = DateTime.SpecifyKind(book.PublicationDate.Value, DateTimeKind.Utc);

            // book.AddedDate es DateTime (no nullable), así que no uses HasValue
            book.AddedDate = DateTime.SpecifyKind(book.AddedDate, DateTimeKind.Utc);

            await _bookRepository.UpdateAsync(book);
            return NoContent();
        }

        // DELETE: api/books/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return NotFound();

            await _bookRepository.DeleteAsync(book);
            return NoContent();
        }
        // Endpoint de búsqueda en Google Books:

        [HttpGet("googlebooks/search")]
        public async Task<IActionResult> SearchGoogleBooks(
            [FromQuery] string? title,
            [FromQuery] string? author,
            [FromQuery] string? language)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(author))
            {
                // En lugar de BadRequest, devuelve lista vacía para mantener estándar
                return Ok(new List<BookDto>());
            }

            var books = await _googleBooksService.SearchGoogleBooksAsync(title, author, language);

            if (books == null || books.Count == 0)
            {
                return Ok(new List<BookDto>()); // No results found, responde con lista vacía
            }

            return Ok(books);
        }



        [HttpPost("import-from-google")]
        [Authorize]
        public async Task<IActionResult> ImportBookFromGoogle([FromBody] ImportBookFromGoogleRequest request)
        {
            try
            {
                _logger.LogInformation("Importando libro con ISBN: {isbn}", request.ISBN);

                // ✅ Validar ISBN
                if (string.IsNullOrWhiteSpace(request.ISBN))
                    return BadRequest("El ISBN es obligatorio.");

                var cleanIsbn = request.ISBN.Replace("-", "").Replace(" ", "");
                if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
                    return BadRequest("El ISBN debe tener 10 o 13 dígitos.");

                // ✅ Obtener UserId del JWT
                var userId = User.GetUserId();
                if (userId == Guid.Empty)
                    return Unauthorized("No se pudo determinar el ID del usuario autenticado.");

                // 🔍 Buscar primero en Google Books
                var googleBook = await _googleBooksService.SearchByIsbnAsync(cleanIsbn);

                // 🔍 Si Google no tiene o tiene datos incompletos, consulta OpenLibrary
                BookDto? openLibraryBook = null;
                if (googleBook == null || HasMissingFields(googleBook))
                {
                    openLibraryBook = await _openLibraryService.GetByIsbnAsync(cleanIsbn);
                }

                // 🔀 Merge
                var merged = BookDataMerger.Merge(googleBook, openLibraryBook, _logger);
                merged.ISBN = cleanIsbn;
                merged.UserId = userId;

                //_logger.LogInformation("🔍 Resultado del merge: {@merged}", merged);

                // 📸 Descargar portada
                if (!string.IsNullOrWhiteSpace(merged.CoverUrl))
                {
                    try
                    {
                        (merged.CoverUrl, merged.ThumbnailUrl) = await _imageService.DownloadAndSaveCoverAsync(merged.CoverUrl, cleanIsbn);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("⚠️ Error al descargar imagen: {Message}", ex.Message);
                    }
                }

                // 🔎 Verificar si ya existe el libro
                var existingBook = await _bookRepository.GetByIsbnAndUserIdAsync(cleanIsbn, userId);

                if (existingBook != null)
                {
                    var updatedBook = MapBookDtoToBook(merged, existingBook);
                    await _bookRepository.UpdateAsync(updatedBook);

//                    _logger.LogInformation("✅ Libro actualizado: {@book}", updatedBook);

                    return Ok(new
                    {
                        message = $"📘 El libro con ISBN {request.ISBN} ya existía y se ha actualizado.",
                        book = updatedBook
                    });
                }

                // ➕ Si no existe, lo creamos
                //_logger.LogInformation("🚀 DTO final antes de mapear a Book: {@MergedBookDto}", merged);

                var newBook = MapBookDtoToBook(merged);
                await _bookRepository.AddAsync(newBook);

//                _logger.LogInformation("✅ Nuevo libro añadido: {@book}", newBook);

                return CreatedAtAction(nameof(GetById), new { id = newBook.Id }, newBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error inesperado al importar libro.");
                return StatusCode(500, "Error interno del servidor al importar el libro.");
            }
        }

        private bool HasMissingFields(BookDto book)
        {
            return string.IsNullOrWhiteSpace(book.Author)
                || string.IsNullOrWhiteSpace(book.Title)
                || string.IsNullOrWhiteSpace(book.Summary)
                || string.IsNullOrWhiteSpace(book.Publisher)
                || string.IsNullOrWhiteSpace(book.Genre)
                || string.IsNullOrWhiteSpace(book.CoverUrl)
                || !book.PageCount.HasValue
                || !book.PublicationDate.HasValue;
        }


        private Book MapBookDtoToBook(BookDto dto, Book? existingBook = null)
        {
            var book = existingBook ?? new Book
            {
                Id = Guid.NewGuid(),
                AddedDate = DateTime.UtcNow,
                UserId = dto.UserId ?? throw new ArgumentException("UserId no puede ser nulo.")
            };

            // Texto
            book.Title = string.IsNullOrWhiteSpace(dto.Title) ? "Sin titulo" : dto.Title;
            book.Author = string.IsNullOrWhiteSpace(dto.Author) ? "Sin autor" : dto.Author;
            book.Series = dto.Series;
            book.ISBN = dto.ISBN;
            book.Publisher = dto.Publisher;
            book.Genre = dto.Genre;
            book.Summary = dto.Summary;
            book.Language = dto.Language;
            book.Country = dto.Country;
            book.LentTo = dto.LentTo;

            // Imagenes blindadas
            book.CoverUrl = dto.CoverUrl ?? book.CoverUrl ?? "/covers/default.jpg";
            book.ThumbnailUrl = dto.ThumbnailUrl ?? book.ThumbnailUrl ?? "/covers/default_thumb.jpg";

            // Números y fechas
            book.PublicationDate = dto.PublicationDate;
            book.PageCount = dto.PageCount;
            book.StartReadingDate = dto.StartReadingDate;
            book.EndReadingDate = dto.EndReadingDate;

            // Estado siempre actualizable
            book.Status = dto.Status;

//            _logger.LogInformation("📝 Book final mapeado: {@Book}", book);

            return book;
        }










        private JsonElement? SelectBestItemWithThumbnail(JsonElement items)
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("volumeInfo", out var vi) &&
                    vi.TryGetProperty("imageLinks", out var imgLinks) &&
                    imgLinks.TryGetProperty("thumbnail", out _))
                {
                    return item;
                }
            }

            return items[0];
        }

        public class ImportBookFromGoogleRequest
        {
            public string ISBN { get; set; } = string.Empty;
        }

        [HttpPost("upload-cover")]
        public async Task<IActionResult> UploadCover([FromBody] UploadCoverRequest request, [FromServices] IImageService imageService)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl) || string.IsNullOrWhiteSpace(request.Isbn))
                return BadRequest("Faltan datos.");

            (string? fullPath, string? thumbnailPath)? result;

            if (request.ImageUrl.StartsWith("data:image/")) // base64
            {
                result = await imageService.SaveBase64ImageAsync(request.ImageUrl, request.Isbn);
            }
            else // imagen desde URL
            {
                result = await imageService.DownloadAndSaveCoverAsync(request.ImageUrl, request.Isbn);
            }

            if (result is { fullPath: not null })
            {
                return Ok(new
                {
                    relativePath = result.Value.fullPath,
                    thumbnailPath = result.Value.thumbnailPath
                });
            }

            return StatusCode(500, "No se pudo guardar la imagen.");
        }

        // Clase para recibir la petición
        public class UploadCoverRequest
        {
            public required string ImageUrl { get; set; }
            public required string Isbn { get; set; }
        }

        [HttpPut("{id}/manual-update")]
        [Authorize]
        public async Task<IActionResult> UpdateBookManually(Guid id, [FromBody] UpdateBookRequest request)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
                return NotFound("📕 Libro no encontrado.");

            // Verifica que el libro pertenezca al usuario autenticado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("No se pudo identificar al usuario.");

            if (book.UserId != userId)
                return Forbid("⛔ No puedes modificar un libro que no te pertenece.");

            // Aplica actualizaciones solo si se proveen (puedes ajustar lógica según tu criterio)
            book.Title = request.Title ?? book.Title;
            book.Author = request.Author ?? book.Author;
            book.Summary = request.Summary ?? book.Summary;
            book.Publisher = request.Publisher ?? book.Publisher;
            book.Genre = request.Genre ?? book.Genre;
            book.PageCount = request.PageCount ?? book.PageCount;
            book.PublicationDate = request.PublicationDate?.ToUniversalTime() ?? book.PublicationDate;
            book.CoverUrl = request.CoverUrl ?? book.CoverUrl;
            book.Language = request.Language ?? book.Language;
            book.Country = request.Country ?? book.Country;
            book.Status = request.Status ?? book.Status;

            await _bookRepository.UpdateAsync(book);

            return Ok(book);
        }
        // En BooksController.cs
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetMyBooks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("No se pudo determinar el ID del usuario autenticado.");

            var books = await _bookRepository.GetBooksByUserIdAsync(userId);
            return Ok(_mapper.Map<IEnumerable<BookDto>>(books));
        }
       

        private BookDto? TryExtractBestGoogleItem(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                return null;

            var item = items.EnumerateArray().FirstOrDefault();
            if (!item.TryGetProperty("volumeInfo", out var vi)) return null;

            return new BookDto
            {
                Title = vi.TryGetProperty("title", out var title) ? title.GetString() : null,
                Author = vi.TryGetProperty("authors", out var authors) && authors.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", authors.EnumerateArray().Select(a => a.GetString()))
                    : null,
                Publisher = vi.TryGetProperty("publisher", out var publisher) ? publisher.GetString() : null,
                Summary = vi.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                PageCount = vi.TryGetProperty("pageCount", out var pages) ? pages.GetInt32() : (int?)null,
                Genre = vi.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", cats.EnumerateArray().Select(c => c.GetString()))
                    : null,
                PublicationDate = vi.TryGetProperty("publishedDate", out var date) &&
                                  DateTime.TryParse(date.GetString(), out var pubDate)
                    ? pubDate.ToUniversalTime()
                    : null,
                Language = vi.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                ThumbnailUrl = vi.TryGetProperty("imageLinks", out var images) &&
                               images.TryGetProperty("thumbnail", out var thumb)
                    ? thumb.GetString()
                    : null,
                Country = item.TryGetProperty("accessInfo", out var access) &&
                          access.TryGetProperty("country", out var country)
                    ? country.GetString()
                    : null
            };
        }

    }
    public static class JsonExtensions
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) ? value : (JsonElement?)null;
        }

        public static string? GetStringArrayAsString(this JsonElement? element)
        {
            return element.HasValue && element.Value.ValueKind == JsonValueKind.Array
                ? string.Join(", ", element.Value.EnumerateArray().Select(x => x.GetString()))
                : null;
        }

        public static DateTime? TryParseDateTimeUtc(this JsonElement? element)
        {
            if (element.HasValue && DateTime.TryParse(element.Value.GetString(), out var dt))
                return dt.ToUniversalTime();
            return null;
        }

    }

}
