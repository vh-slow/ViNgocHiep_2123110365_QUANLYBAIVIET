using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;
using ViNgocHiep_2123110365.Helpers;
using ViNgocHiep_2123110365.Models;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api/admin/books")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminBooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminBooksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/books
        [HttpGet]
        public async Task<ActionResult<PagedResponse<IEnumerable<BookListResponseDTO>>>> GetBooks(
            [FromQuery] AdminBookFilter filter
        )
        {
            var query = _context
                .Books.IgnoreQueryFilters()
                .Include(b => b.Category)
                .Include(b => b.User)
                .Include(b => b.Favorites)
                .Include(b => b.BookTags!)
                .ThenInclude(bt => bt.Tag)
                .AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(b => b.Status == filter.Status.Value);
            if (filter.IsDeleted.HasValue)
                query = query.Where(b => b.IsDeleted == filter.IsDeleted.Value);
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                query = query.Where(b =>
                    b.Title.ToLower().Contains(filter.SearchQuery.ToLower().Trim())
                );

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BookListResponseDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Thumbnail = b.Thumbnail,
                    Summary = b.Summary,
                    Status = b.Status,
                    ViewCount = b.ViewCount,
                    CreatedAt = b.CreatedAt,
                    IsDeleted = b.IsDeleted,
                    FavoriteCount = b.Favorites!.Count,
                    Tags = b.BookTags!.Select(bt => bt.Tag!.Name).ToList(),
                    Category = new CategoryDTO { Id = b.Category!.Id, Name = b.Category.Name },
                    User = new UserDTO
                    {
                        Id = b.User!.Id,
                        FullName = b.User.FullName,
                        Username = b.User.Username,
                    },
                })
                .ToListAsync();

            return Ok(
                new PagedResponse<IEnumerable<BookListResponseDTO>>(
                    pagedData,
                    filter.PageNumber,
                    filter.PageSize,
                    totalRecords
                )
            );
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDetailResponseDTO>> GetBook(int id)
        {
            var book = await _context
                .Books.IgnoreQueryFilters()
                .Include(b => b.Category)
                .Include(b => b.User)
                .Include(b => b.Favorites)
                .Include(b => b.BookTags!)
                .ThenInclude(bt => bt.Tag)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound(new { message = "Không tìm thấy bài viết" });

            var response = new BookDetailResponseDTO
            {
                Id = book.Id,
                Title = book.Title,
                Slug = book.Slug,
                Thumbnail = book.Thumbnail,
                Summary = book.Summary,
                Content = book.Content,
                ViewCount = book.ViewCount,
                Status = book.Status,
                CreatedAt = book.CreatedAt,
                FavoriteCount = book.Favorites!.Count,
                Tags = book.BookTags!.Select(bt => bt.Tag!.Name).ToList(),
                Category = new CategoryDTO { Id = book.Category!.Id, Name = book.Category.Name },
                User = new UserDTO
                {
                    Id = book.User!.Id,
                    FullName = book.User.FullName,
                    Avatar = book.User.Avatar,
                    Username = book.User.Username,
                },
            };

            return Ok(response);
        }

        // POST: api/admin/books
        [HttpPost]
        public async Task<IActionResult> PostBook([FromForm] CreateUpdateBookDTO request)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var book = new Book
            {
                Title = request.Title,
                Slug = StringHelper.GenerateSlug(request.Title),
                Summary = request.Summary,
                Content = request.Content,
                CategoryId = request.CategoryId,
                Thumbnail = await FileHelper.UploadFileAsync(request.ThumbnailFile, "books"),
                UserId = adminId,
                CreatedAt = DateTime.Now,
                Status = 1,
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            if (request.TagNames != null && request.TagNames.Any())
            {
                await ProcessTags(book.Id, request.TagNames);
            }

            return Ok(new { success = true, message = "Đã đăng bài viết thành công." });
        }

        // PUT: api/admin/books/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, [FromForm] CreateUpdateBookDTO request)
        {
            var oldBook = await _context
                .Books.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);
            if (oldBook == null)
                return NotFound();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            _context.BookHistories.Add(
                new BookHistory
                {
                    BookId = id,
                    OldContent = oldBook.Content,
                    EditedByUserId = adminId,
                    CreatedAt = DateTime.Now,
                }
            );

            var oldTags = _context.BookTags.Where(bt => bt.BookId == id);
            _context.BookTags.RemoveRange(oldTags);
            if (request.TagNames != null && request.TagNames.Any())
            {
                await ProcessTags(id, request.TagNames);
            }

            var updatedBook = new Book
            {
                Id = id,
                Title = request.Title,
                Slug = StringHelper.GenerateSlug(request.Title),
                Summary = request.Summary,
                Content = request.Content,
                CategoryId = request.CategoryId,
                Thumbnail =
                    request.ThumbnailFile != null
                        ? await FileHelper.UploadFileAsync(request.ThumbnailFile, "books")
                        : oldBook.Thumbnail,
                UserId = oldBook.UserId,
                CreatedAt = oldBook.CreatedAt,
                UpdatedAt = DateTime.Now,
                Status = oldBook.Status,
                IsDeleted = oldBook.IsDeleted,
            };

            _context.Entry(updatedBook).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật bài viết thành công." });
        }

        private async Task ProcessTags(int bookId, List<string> tagNames)
        {
            foreach (var name in tagNames)
            {
                var trimmedName = name.Trim();
                if (string.IsNullOrEmpty(trimmedName))
                    continue;

                var tag = await _context.Tags.FirstOrDefaultAsync(t =>
                    t.Name.ToLower() == trimmedName.ToLower()
                );
                if (tag == null)
                {
                    tag = new Tag
                    {
                        Name = trimmedName,
                        Slug = StringHelper.GenerateSlug(trimmedName),
                        CreatedAt = DateTime.Now,
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }
                _context.BookTags.Add(new BookTag { BookId = bookId, TagId = tag.Id });
            }
            await _context.SaveChangesAsync();
        }

        // DELETE: api/admin/books/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context
                .Books.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return NotFound();

            book.IsDeleted = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã khóa bài viết." });
        }

        // PUT: api/admin/books/{id}/lock
        [HttpPut("{id}/lock")]
        public async Task<IActionResult> LockBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            book.Status = 3;
            book.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã khóa bài viết vi phạm." });
        }

        // PUT: api/admin/books/{id}/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();
            book.Status = 1;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã duyệt bài viết thành công." });
        }

        // PUT: api/admin/books/{id}/restore
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreBook(int id)
        {
            var book = await _context
                .Books.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return NotFound();
            book.IsDeleted = false;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã khôi phục bài viết." });
        }

        // GET: api/admin/books/{id}/history
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<BookHistoryDTO>>> GetBookHistoryByAdmin(int id)
        {
            var bookExists = await _context.Books.IgnoreQueryFilters().AnyAsync(b => b.Id == id);
            if (!bookExists)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            var history = await _context
                .BookHistories.Where(h => h.BookId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Join(
                    _context.Users.IgnoreQueryFilters(),
                    h => h.EditedByUserId,
                    u => u.Id,
                    (h, u) =>
                        new BookHistoryDTO
                        {
                            Id = h.Id,
                            BookId = h.BookId,
                            OldContent = h.OldContent,
                            CreatedAt = h.CreatedAt,
                            EditedByUserId = h.EditedByUserId,
                            EditedByUserName = u.FullName,
                        }
                )
                .ToListAsync();

            return Ok(history);
        }
    }
}
