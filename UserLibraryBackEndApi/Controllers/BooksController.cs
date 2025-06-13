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

        public BooksController(
            IBooksRepository bookRepository,
            IMapper mapper,
            GoogleBooksService googleBooksService,
            ILogger<BooksController> logger,
            IImageService imageService)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
            _googleBooksService = googleBooksService;
            _logger = logger;
            _imageService = imageService;
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
            [FromQuery] string? language, 
            [FromQuery] string? country)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(author))
                return BadRequest("Debes indicar al menos un título o un autor.");

            var query = "";
            if (!string.IsNullOrWhiteSpace(title)) query += $"intitle:{title} ";
            if (!string.IsNullOrWhiteSpace(author)) query += $"inauthor:{author}";

            var result = await _googleBooksService.SearchAsync(query.Trim(), language, country);
            return Content(result, "application/json");
        }


        //[HttpPost("import-from-google")]
        //[Authorize]
        //public async Task<IActionResult> ImportBookFromGoogle([FromBody] ImportBookFromGoogleRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.ISBN))
        //        return BadRequest("El ISBN es obligatorio.");

        //    var cleanIsbn = request.ISBN.Replace("-", "").Replace(" ", "");
        //    if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
        //        return BadRequest("El ISBN debe tener 10 o 13 dígitos.");

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
        //    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        //        return Unauthorized("No se pudo determinar el ID del usuario autenticado.");

        //    // Búsqueda solo por ISBN
        //    var resultJson = await _googleBooksService.SearchAsync($"isbn:{cleanIsbn}");
        //    _logger.LogInformation("📚 JSON recibido de Google Books para ISBN {ISBN}: {Json}", cleanIsbn, resultJson);

        //    var result = JsonDocument.Parse(resultJson);
        //    if (!result.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        //        return NotFound($"No se encontró ningún libro con ISBN {request.ISBN}.");

        //    JsonElement? selectedItem = null;

        //    foreach (var item in items.EnumerateArray())
        //    {
        //        if (item.TryGetProperty("volumeInfo", out var vi) &&
        //            vi.TryGetProperty("imageLinks", out var imgLinks) &&
        //            imgLinks.TryGetProperty("thumbnail", out _))
        //        {
        //            selectedItem = item;
        //            break; // toma el primero que tenga portada
        //        }
        //    }

        //    // Si no encontró con portada, usa la primera
        //    selectedItem ??= items[0];

        //    var volume = selectedItem.Value.GetProperty("volumeInfo");
        //    var accessInfo = selectedItem.Value.GetProperty("accessInfo");

        //    string? fullCoverUrl = null;
        //    string? thumbnailCoverUrl = null;
        //    if (volume.TryGetProperty("imageLinks", out var imageLinks) &&
        //        imageLinks.TryGetProperty("thumbnail", out var thumbnailProp))
        //    {
        //        var thumbnailUrl = thumbnailProp.GetString();
        //        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        //        {
        //            try
        //            {
        //                (fullCoverUrl, thumbnailCoverUrl) = await _imageService.DownloadAndSaveCoverAsync(thumbnailUrl, cleanIsbn);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogWarning("Error al descargar o guardar la imagen de portada: {Message}", ex.Message);
        //            }
        //        }
        //    }

        //    var existingBook = await _bookRepository.GetByIsbnAndUserIdAsync(cleanIsbn, userId);

        //    if (existingBook != null)
        //    {
        //        existingBook.Title = volume.GetProperty("title").GetString() ?? existingBook.Title;
        //        existingBook.Author = volume.TryGetProperty("authors", out var authors) &&
        //                              authors.ValueKind == JsonValueKind.Array && authors.GetArrayLength() > 0
        //            ? string.Join(", ", authors.EnumerateArray().Select(a => a.GetString()))
        //            : existingBook.Author;
        //        existingBook.Publisher = volume.TryGetProperty("publisher", out var publisher)
        //            ? publisher.GetString()
        //            : existingBook.Publisher;
        //        existingBook.Summary = volume.TryGetProperty("description", out var desc)
        //            ? desc.GetString()
        //            : existingBook.Summary;
        //        existingBook.CoverUrl = fullCoverUrl ?? existingBook.CoverUrl;
        //        existingBook.ThumbnailUrl = thumbnailCoverUrl ?? existingBook.ThumbnailUrl;

        //        existingBook.PageCount = volume.TryGetProperty("pageCount", out var pages)
        //            ? pages.GetInt32()
        //            : existingBook.PageCount;
        //        existingBook.Genre = volume.TryGetProperty("categories", out var categories)
        //            ? string.Join(", ", categories.EnumerateArray().Select(c => c.GetString()))
        //            : existingBook.Genre;
        //        existingBook.PublicationDate = volume.TryGetProperty("publishedDate", out var date) &&
        //                                       DateTime.TryParse(date.GetString(), out var pubDate)
        //            ? pubDate.ToUniversalTime()
        //            : existingBook.PublicationDate;
        //        existingBook.Language = volume.TryGetProperty("language", out var lang)
        //            ? lang.GetString()
        //            : existingBook.Language;
        //        existingBook.Country = accessInfo.TryGetProperty("country", out var country)
        //            ? country.GetString()
        //            : existingBook.Country;

        //        await _bookRepository.UpdateAsync(existingBook);

        //        return Ok(new
        //        {
        //            message = $"📘 El libro con ISBN {request.ISBN} ya existía y se ha actualizado.",
        //            book = existingBook
        //        });
        //    }
        //    else
        //    {
        //        var newBook = new Book
        //        {
        //            Id = Guid.NewGuid(),
        //            UserId = userId,
        //            Title = volume.GetProperty("title").GetString() ?? "Sin título",
        //            Author = volume.TryGetProperty("authors", out var authors) &&
        //                     authors.ValueKind == JsonValueKind.Array && authors.GetArrayLength() > 0
        //                ? string.Join(", ", authors.EnumerateArray().Select(a => a.GetString()))
        //                : "Desconocido",
        //            ISBN = cleanIsbn,
        //            Publisher = volume.TryGetProperty("publisher", out var publisher) ? publisher.GetString() : null,
        //            Summary = volume.TryGetProperty("description", out var desc) ? desc.GetString() : null,
        //            CoverUrl = fullCoverUrl,
        //            ThumbnailUrl = thumbnailCoverUrl,
        //            PageCount = volume.TryGetProperty("pageCount", out var pages) ? pages.GetInt32() : (int?)null,
        //            Genre = volume.TryGetProperty("categories", out var categories)
        //                ? string.Join(", ", categories.EnumerateArray().Select(c => c.GetString()))
        //                : null,
        //            PublicationDate = volume.TryGetProperty("publishedDate", out var date) &&
        //                              DateTime.TryParse(date.GetString(), out var pubDate)
        //                ? pubDate.ToUniversalTime()
        //                : (DateTime?)null,
        //            Status = ReadingStatus.NotRead,
        //            AddedDate = DateTime.UtcNow,
        //            Language = volume.TryGetProperty("language", out var lang) ? lang.GetString() : null,
        //            Country = accessInfo.TryGetProperty("country", out var country) ? country.GetString() : null
        //        };

        //        await _bookRepository.AddAsync(newBook);

        //        return CreatedAtAction(nameof(GetById), new { id = newBook.Id }, newBook);
        //    }
        //}
        [HttpPost("import-from-google")]
        [Authorize]
        public async Task<IActionResult> ImportBookFromGoogle([FromBody] ImportBookFromGoogleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ISBN))
                return BadRequest("El ISBN es obligatorio.");

            var cleanIsbn = request.ISBN.Replace("-", "").Replace(" ", "");
            if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
                return BadRequest("El ISBN debe tener 10 o 13 dígitos.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("No se pudo determinar el ID del usuario autenticado.");

            // Primera búsqueda por ISBN
            var resultJson = await _googleBooksService.SearchAsync($"isbn:{cleanIsbn}");
            _logger.LogInformation("📚 JSON recibido de Google Books para ISBN {ISBN}: {Json}", cleanIsbn, resultJson);

            var result = JsonDocument.Parse(resultJson);

            if (!result.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                return NotFound($"No se encontró ningún libro con ISBN {request.ISBN}.");

            JsonElement? selectedItem = null;

            // Escogemos el primer item con thumbnail si existe
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("volumeInfo", out var vi) &&
                    vi.TryGetProperty("imageLinks", out var imgLinks) &&
                    imgLinks.TryGetProperty("thumbnail", out _))
                {
                    selectedItem = item;
                    break;
                }
            }
            selectedItem ??= items[0];

            var volume = selectedItem.Value.GetProperty("volumeInfo");

            // Extraemos datos principales para validar
            string? title = volume.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
            string? author = (volume.TryGetProperty("authors", out var authors) &&
                              authors.ValueKind == JsonValueKind.Array &&
                              authors.GetArrayLength() > 0)
                ? string.Join(", ", authors.EnumerateArray().Select(a => a.GetString()))
                : null;

            // Si no hay portada o autor, intentamos buscar por título + autor para completar datos
            bool missingImportantData = false;

            if (author == null ||
                !(volume.TryGetProperty("imageLinks", out var imageLinks) && imageLinks.TryGetProperty("thumbnail", out var thumbnailProp)))
            {
                missingImportantData = true;
            }

            if (missingImportantData && !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(author))
            {
                var query = $"{title} {author}";
                _logger.LogInformation("🔄 Faltan datos, buscando con título y autor: {Query}", query);
                var secondaryResultJson = await _googleBooksService.SearchAsync(query);
                var secondaryResult = JsonDocument.Parse(secondaryResultJson);

                if (secondaryResult.RootElement.TryGetProperty("items", out var secondaryItems) && secondaryItems.GetArrayLength() > 0)
                {
                    // Tomamos el primer resultado relevante con portada
                    foreach (var item in secondaryItems.EnumerateArray())
                    {
                        if (item.TryGetProperty("volumeInfo", out var vi) &&
                            vi.TryGetProperty("imageLinks", out var imgLinks) &&
                            imgLinks.TryGetProperty("thumbnail", out _))
                        {
                            selectedItem = item;
                            volume = selectedItem.Value.GetProperty("volumeInfo");
                            _logger.LogInformation("volumeInfo: {volume}", volume);
                            break;
                        }
                    }
                }
            }

            var accessInfo = selectedItem.Value.GetProperty("accessInfo");

            // Descarga y guarda imagenes
            string? fullCoverUrl = null;
            string? thumbnailCoverUrl = null;
            if (volume.TryGetProperty("imageLinks", out imageLinks) &&
                imageLinks.TryGetProperty("thumbnail", out var thumbnail))
            {
                var thumbnailUrl = thumbnail.GetString();
                if (!string.IsNullOrWhiteSpace(thumbnailUrl))
                {
                    try
                    {
                        (fullCoverUrl, thumbnailCoverUrl) = await _imageService.DownloadAndSaveCoverAsync(thumbnailUrl, cleanIsbn);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error al descargar o guardar la imagen de portada: {Message}", ex.Message);
                    }
                }
            }

            var existingBook = await _bookRepository.GetByIsbnAndUserIdAsync(cleanIsbn, userId);

            if (existingBook != null)
            {
                // Actualiza campos
                existingBook.Title = volume.GetProperty("title").GetString() ?? existingBook.Title;
                existingBook.Author = volume.TryGetProperty("authors", out var miauthors) &&
                                      miauthors.ValueKind == JsonValueKind.Array && miauthors.GetArrayLength() > 0
                    ? string.Join(", ", miauthors.EnumerateArray().Select(a => a.GetString()))
                    : existingBook.Author;
                existingBook.Publisher = volume.TryGetProperty("publisher", out var publisher)
                    ? publisher.GetString()
                    : existingBook.Publisher;
                existingBook.Summary = volume.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : existingBook.Summary;
                existingBook.CoverUrl = fullCoverUrl ?? existingBook.CoverUrl;
                existingBook.ThumbnailUrl = thumbnailCoverUrl ?? existingBook.ThumbnailUrl;

                existingBook.PageCount = volume.TryGetProperty("pageCount", out var pages)
                    ? pages.GetInt32()
                    : existingBook.PageCount;
                existingBook.Genre = volume.TryGetProperty("categories", out var categories)
                    ? string.Join(", ", categories.EnumerateArray().Select(c => c.GetString()))
                    : existingBook.Genre;
                existingBook.PublicationDate = volume.TryGetProperty("publishedDate", out var date) &&
                                               DateTime.TryParse(date.GetString(), out var pubDate)
                    ? pubDate.ToUniversalTime()
                    : existingBook.PublicationDate;
                existingBook.Language = volume.TryGetProperty("language", out var lang)
                    ? lang.GetString()
                    : existingBook.Language;
                existingBook.Country = accessInfo.TryGetProperty("country", out var country)
                    ? country.GetString()
                    : existingBook.Country;

                await _bookRepository.UpdateAsync(existingBook);

                return Ok(new
                {
                    message = $"📘 El libro con ISBN {request.ISBN} ya existía y se ha actualizado.",
                    book = existingBook
                });
            }
            else
            {
                var newBook = new Book
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = volume.GetProperty("title").GetString() ?? "Sin título",
                    Author = volume.TryGetProperty("authors", out var miauthors) &&
                             miauthors.ValueKind == JsonValueKind.Array && miauthors.GetArrayLength() > 0
                        ? string.Join(", ", authors.EnumerateArray().Select(a => a.GetString()))
                        : "Desconocido",
                    ISBN = cleanIsbn,
                    Publisher = volume.TryGetProperty("publisher", out var publisher) ? publisher.GetString() : null,
                    Summary = volume.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    CoverUrl = fullCoverUrl,
                    ThumbnailUrl = thumbnailCoverUrl,
                    PageCount = volume.TryGetProperty("pageCount", out var pages) ? pages.GetInt32() : (int?)null,
                    Genre = volume.TryGetProperty("categories", out var categories)
                        ? string.Join(", ", categories.EnumerateArray().Select(c => c.GetString()))
                        : null,
                    PublicationDate = volume.TryGetProperty("publishedDate", out var date) &&
                                      DateTime.TryParse(date.GetString(), out var pubDate)
                        ? pubDate.ToUniversalTime()
                        : (DateTime?)null,
                    Status = ReadingStatus.NotRead,
                    AddedDate = DateTime.UtcNow,
                    Language = volume.TryGetProperty("language", out var lang) ? lang.GetString() : null,
                    Country = accessInfo.TryGetProperty("country", out var country) ? country.GetString() : null
                };

                await _bookRepository.AddAsync(newBook);

                return CreatedAtAction(nameof(GetById), new { id = newBook.Id }, newBook);
            }
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

            var (fullPath, thumbnailPath) = await imageService.DownloadAndSaveCoverAsync(request.ImageUrl, request.Isbn);

            if (string.IsNullOrEmpty(fullPath))
                return StatusCode(500, "No se pudo guardar la imagen.");

            // Devuelve la ruta relativa para guardar en la base de datos
            return Ok(new { relativePath = fullPath, thumbnailPath });
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

    }
}



