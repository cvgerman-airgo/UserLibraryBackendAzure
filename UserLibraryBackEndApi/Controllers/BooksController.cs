using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SixLabors.ImageSharp;
using Application.Interfaces;
using Application.Helpers;
using Infrastructure.Estensions;
using CsvHelper;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using System.Text;
using CsvHelper.Configuration;
using Infrastructure.Repositories;

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
        private readonly IOpenLibraryService _openLibraryService;
        private readonly ApplicationDbContext _context;

        public BooksController(
            IBooksRepository bookRepository,
            IMapper mapper,
            GoogleBooksService googleBooksService,
            ILogger<BooksController> logger,
            IOpenLibraryService openLibraryService,
            ApplicationDbContext context)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
            _googleBooksService = googleBooksService;
            _logger = logger;
            _openLibraryService = openLibraryService;
            _context = context;
        }

        // GET: api/books/internet-search?title=...&author=...&language=...
        [HttpGet("internet-search")]
        public async Task<IActionResult> InternetSearch([FromQuery] string? title, [FromQuery] string? author, [FromQuery] string? language)
        {
            var googleResults = await _googleBooksService.SearchGoogleBooksAsync(title, author, language);
            var openResults = await _openLibraryService.SearchOpenLibraryAsync(title, author, language);
            var allResults = new List<BookDto>();
            if (googleResults != null) allResults.AddRange(googleResults);
            if (openResults != null) allResults.AddRange(openResults);
            // Normalizar y filtrar duplicados por ISBN
            var normalized = allResults
                .Where(b => !string.IsNullOrWhiteSpace(b.Title) && !string.IsNullOrWhiteSpace(b.Author))
                .GroupBy(b => b.ISBN ?? b.Title)
                .Select(g => g.First())
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    Title = b.Title ?? "",
                    Series = b.Series,
                    ISBN = b.ISBN ?? "",
                    CoverUrl = b.CoverUrl,
                    ThumbnailUrl = b.ThumbnailUrl,
                    Author = b.Author ?? "",
                    Publisher = b.Publisher ?? "",
                    Genre = b.Genre,
                    Summary = b.Summary,
                    PublicationDate = b.PublicationDate,
                    PageCount = b.PageCount,
                    Country = b.Country,
                    Language = b.Language,
                    AddedDate = b.AddedDate,
                    StartReadingDate = b.StartReadingDate,
                    EndReadingDate = b.EndReadingDate,
                    Status = b.Status,
                    LentTo = b.LentTo,
                    CoverImage = b.CoverImage
                })
                .ToList();
            return Ok(normalized);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportBooksCsv()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var books = await _context.Books
                .Where(b => b.UserId == Guid.Parse(userId))
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    Title = b.Title,
                    Series = b.Series,
                    ISBN = b.ISBN,
                    Author = b.Author,
                    Publisher = b.Publisher,
                    Genre = b.Genre,
                    Summary = b.Summary,
                    PublicationDate = b.PublicationDate,
                    PageCount = b.PageCount,
                    Country = b.Country,
                    Language = b.Language,
                    AddedDate = b.AddedDate,
                    StartReadingDate = b.StartReadingDate,
                    EndReadingDate = b.EndReadingDate,
                    Status = b.Status,
                    LentTo = b.LentTo
                })
                .ToListAsync();

            using var writer = new StringWriter();
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(books);
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(writer.ToString());
            return File(csvBytes, "text/csv", "books.csv");
        }


        [HttpPost("import")]
        [Authorize]
        public async Task<IActionResult> ImportBooks(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha enviado ningún archivo.");

            try
            {
                // Obtener ID del usuario autenticado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized();

                var userId = Guid.Parse(userIdClaim);

                int addedCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;

                // Configuración CSV
                var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",           // CSV delimitado por punto y coma
                    HeaderValidated = null,    // Ignora errores si headers no coinciden
                    MissingFieldFound = null,  // Ignora errores si faltan campos
                    BadDataFound = null        // Ignora datos malos
                };

                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
                using var csv = new CsvHelper.CsvReader(reader, config);

                // Registrar mapping si usas BookCsvMap
                csv.Context.RegisterClassMap<BookCsvMap>();

                // Configurar formatos de fecha para todas las columnas DateTime / DateTime?
                var dateFormats = new[]
                {
            "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss",
            "dd/MM/yyyy HH:mm",    "MM/dd/yyyy HH:mm",    "yyyy-MM-dd HH:mm",
            "dd/MM/yyyy H:mm",     "MM/dd/yyyy H:mm",     "yyyy-MM-dd H:mm",
            "yyyy-MM-dd",          "MM/dd/yyyy",          "dd/MM/yyyy"
        };
                csv.Context.TypeConverterOptionsCache.GetOptions<DateTime?>().Formats = dateFormats;
                csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = dateFormats;

                // Leer todos los registros
                var records = csv.GetRecords<BookDto>().ToList();

                foreach (var record in records)
                {
                    // Validar campos obligatorios
                    if (string.IsNullOrWhiteSpace(record.Title) || string.IsNullOrWhiteSpace(record.Author))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Sobrescribir UserId con el del usuario autenticado
                    record.UserId = userId;

                    // Si AddedDate es vacío, asignar fecha UTC actual
                    if (record.AddedDate == default)
                        record.AddedDate = DateTime.UtcNow;

                    // Convertir todas las fechas a UTC
                    DateTimeHelper.FixDateTimesToUtc(record);

                    // Buscar libro existente solo si el ISBN no es nulo o vacío
                    var isbnValue = record.ISBN ?? string.Empty;
                    var existingBook = !string.IsNullOrWhiteSpace(isbnValue)
                        ? await _bookRepository.GetByUserIdAndIsbnAsync(userId, isbnValue)
                        : null;

                    if (existingBook != null)
                    {
                        // Actualizar libro existente
                        existingBook.Title = record.Title;
                        existingBook.Series = record.Series;
                        existingBook.Author = record.Author;
                        existingBook.Publisher = record.Publisher;
                        existingBook.Genre = record.Genre;
                        existingBook.Summary = record.Summary;
                        existingBook.PublicationDate = record.PublicationDate;
                        existingBook.PageCount = record.PageCount;
                        existingBook.Country = record.Country;
                        existingBook.Language = record.Language;
                        existingBook.AddedDate = record.AddedDate;
                        existingBook.StartReadingDate = record.StartReadingDate;
                        existingBook.EndReadingDate = record.EndReadingDate;
                        existingBook.Status = record.Status;
                        existingBook.LentTo = record.LentTo;

                        await _bookRepository.UpdateAsync(existingBook);
                        updatedCount++;
                    }
                    else
                    {
                        // Insertar nuevo libro
                        var bookEntity = _mapper.Map<Book>(record);
                        await _bookRepository.AddAsync(bookEntity);
                        addedCount++;
                    }
                }

                // Guardar cambios en la base de datos
                await _bookRepository.SaveChangesAsync();

                // Retornar mensaje bonito y resumido para el frontend
                return Ok(new
                {
                    added = addedCount,
                    updated = updatedCount,
                    skipped = skippedCount,
                    Message = $"Importación completada ✅\n" +
                              $"Libros añadidos: {addedCount}\n" +
                              $"Libros actualizados: {updatedCount}\n" +
                              $"Registros omitidos: {skippedCount}"
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return StatusCode(500, $"Error al importar CSV: {ex.Message} - {inner}");
            }
        }

        // http://localhost:5000/api/Books/data
        [HttpGet("data")]
        [Authorize]  // ✅ así solo usuarios logueados pueden acceder
        public IActionResult GetToolsData()
        {
            var publishers = _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Publisher))
                .Select(b => b.Publisher)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var authors = _context.Books
                .Where(b => !string.IsNullOrEmpty(b.Author))
                .Select(b => b.Author)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            return Ok(new
            {
                publishers,
                authors,
                publisherMap = new Dictionary<string, string>(),
                authorMap = new Dictionary<string, string>()
            });
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
            Console.WriteLine("=================");
            Console.WriteLine($"PUT RECIBIDO PARA: {id}");
            Console.WriteLine($"REQUEST IMAGEN: {request.CoverImage?.Length ?? 0} bytes");
            Console.WriteLine("=================");

            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return NotFound();
            try
            {
                _mapper.Map(request, book);
                book.CoverImage = request.CoverImage;
                Console.WriteLine($"ANTES DE GUARDAR: {book.CoverImage?.Length ?? 0} bytes");

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
                var savedBook = await _bookRepository.GetByIdAsync(id);
                if (savedBook != null)
                    Console.WriteLine($"DESPUES DE GUARDAR: {savedBook.CoverImage?.Length ?? 0} bytes");
                else
                    Console.WriteLine("DESPUES DE GUARDAR: libro no encontrado");
                return Ok(savedBook);
            }
            catch (Exception ex)
            { _logger.LogError(ex, "Error al actualizar el libro con ISBN: {ISBN}", book.ISBN );
                return BadRequest("Error al actualizar el libro: " + ex.Message);
            }
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

                if (string.IsNullOrWhiteSpace(request.ISBN))
                    return BadRequest(new { error = "El ISBN es obligatorio." });

                var cleanIsbn = request.ISBN.Replace("-", "").Replace(" ", "");
                if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
                    return BadRequest(new { error = "El ISBN debe tener 10 o 13 dígitos." });

                var userId = User.GetUserId();
                if (userId == Guid.Empty)
                    return Unauthorized(new { error = "No se pudo determinar el ID del usuario autenticado." });

                // Buscar en Google Books y OpenLibrary
                var googleBook = await _googleBooksService.SearchByIsbnAsync(cleanIsbn);
                BookDto? openLibraryBook = null;
                if (googleBook == null || HasMissingFields(googleBook))
                {
                    openLibraryBook = await _openLibraryService.GetByIsbnAsync(cleanIsbn);
                }
                var merged = BookDataMerger.Merge(googleBook, openLibraryBook, _logger);
                merged.ISBN = cleanIsbn;
                merged.UserId = userId;

                // Descargar ambas imágenes y guardar la más pequeña en CoverImage
                async Task<byte[]?> DownloadImageAsync(string? url)
                {
                    if (string.IsNullOrWhiteSpace(url)) return null;
                    try
                    {
                        using var httpClient = new HttpClient();
                        var imgBytes = await httpClient.GetByteArrayAsync(url);
                        return imgBytes;
                    }
                    catch { return null; }
                }

                byte[]? coverBytes = await DownloadImageAsync(merged.CoverUrl);
                byte[]? thumbBytes = await DownloadImageAsync(merged.ThumbnailUrl);

                if (coverBytes != null && thumbBytes != null)
                    merged.CoverImage = coverBytes.Length <= thumbBytes.Length ? coverBytes : thumbBytes;
                else if (coverBytes != null)
                    merged.CoverImage = coverBytes;
                else if (thumbBytes != null)
                    merged.CoverImage = thumbBytes;
                else
                    merged.CoverImage = null;

                // Verificar si ya existe el libro
                var existingBook = await _bookRepository.GetByIsbnAndUserIdAsync(cleanIsbn, userId);
                if (existingBook != null)
                {
                    var updatedBook = MapBookDtoToBook(merged, existingBook);
                    await _bookRepository.UpdateAsync(updatedBook);
                    var updatedBookDto = _mapper.Map<BookDto>(updatedBook);
                    return Ok(new
                    {
                        message = $"📘 El libro con ISBN {request.ISBN} ya existía y se ha actualizado.",
                        book = updatedBookDto
                    });
                }

                // Crear nuevo libro
                var newBook = MapBookDtoToBook(merged);
                await _bookRepository.AddAsync(newBook);
                var newBookDto = _mapper.Map<BookDto>(newBook);
                return Ok(new
                {
                    message = $"📗 El libro con ISBN {request.ISBN} se ha importado correctamente.",
                    book = newBookDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error inesperado al importar libro.");
                return StatusCode(500, new { error = "Error interno del servidor al importar el libro." });
            }
        }

        private bool HasMissingFields(BookDto book)
        {
            return string.IsNullOrWhiteSpace(book.Author)
                || string.IsNullOrWhiteSpace(book.Title)
                || string.IsNullOrWhiteSpace(book.Summary)
                || string.IsNullOrWhiteSpace(book.Publisher)
                || string.IsNullOrWhiteSpace(book.Genre)
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
            // Imagen
            book.CoverImage = dto.CoverImage;

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

        public class ImportBookFromGoogleRequest
        {
            public string ISBN { get; set; } = string.Empty;
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
