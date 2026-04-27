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
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                return uid;
            return null;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<IEnumerable<BookListResponseDTO>>>> GetBooks(
            [FromQuery] PublicBookFilter filter
        )
        {
            var currentUserId = GetCurrentUserId();
            var query = _context
                .Books.Include(b => b.Category)
                .Include(b => b.User)
                .Include(b => b.Favorites)
                .Where(b => b.Status == 1 && b.Category != null && !b.Category.IsDeleted)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == filter.CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var search = filter.SearchQuery.ToLower().Trim();
                query = query.Where(b =>
                    b.Title.ToLower().Contains(search)
                    || (b.Summary != null && b.Summary.ToLower().Contains(search))
                );
            }

            query =
                filter.SortBy?.ToLower() == "view_desc"
                    ? query.OrderByDescending(b => b.ViewCount)
                    : query.OrderByDescending(b => b.CreatedAt);

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BookListResponseDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Thumbnail = b.Thumbnail,
                    Summary = b.Summary,
                    ViewCount = b.ViewCount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,

                    IsFavorited =
                        currentUserId.HasValue
                        && b.Favorites.Any(f => f.UserId == currentUserId.Value),

                    FavoriteCount = b.Favorites.Count,

                    Category = new CategoryDTO
                    {
                        Id = b.Category!.Id,
                        Name = b.Category.Name,
                        Slug = b.Category.Slug,
                    },
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

        // GET: api/Books/{slug}
        [HttpGet("{slug}")]
        public async Task<ActionResult<BookDetailResponseDTO>> GetBook(string slug)
        {
            var currentUserId = GetCurrentUserId();
            var book = await _context
                .Books.Include(b => b.Category)
                .Include(b => b.User)
                .Include(b => b.Favorites)
                .Include(b => b.Comments!)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Slug == slug);

            if (book == null)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            var isAdmin = User.IsInRole("admin");

            if (book.Status != 1)
            {
                if (!currentUserId.HasValue || (currentUserId.Value != book.UserId && !isAdmin))
                {
                    return Forbid();
                }
            }

            var bookDetail = new BookDetailResponseDTO
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

                IsFavorited =
                    currentUserId.HasValue
                    && book.Favorites!.Any(f => f.UserId == currentUserId.Value),

                FavoriteCount = book.Favorites!.Count,

                Category = new CategoryDTO
                {
                    Id = book.Category!.Id,
                    Name = book.Category.Name,
                    Slug = book.Category.Slug,
                },
                User = new UserDTO
                {
                    Id = book.User!.Id,
                    FullName = book.User.FullName,
                    Username = book.User.Username,
                    Avatar = book.User.Avatar,
                },
                Comments = book.Comments!.Select(c => new CommentDTO
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        BookId = c.BookId,
                        User = new UserDTO
                        {
                            Id = c.User!.Id,
                            FullName = c.User.FullName,
                            Username = c.User.Username,
                            Avatar = c.User.Avatar,
                        },
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList(),
            };
            return Ok(bookDetail);
        }

        [Authorize(Roles = "user,admin")]
        [HttpGet("detail/{id}")]
        public async Task<ActionResult<BookDetailResponseDTO>> GetBookByIdForEdit(int id)
        {
            var currentUserId = GetCurrentUserId()!.Value;
            var isAdmin = User.IsInRole("admin");

            var book = await _context
                .Books.Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null || book.IsDeleted)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            if (!isAdmin && book.UserId != currentUserId)
                return Forbid();

            return Ok(
                new BookDetailResponseDTO
                {
                    Id = book.Id,
                    Title = book.Title,
                    Summary = book.Summary,
                    Content = book.Content,
                    Thumbnail = book.Thumbnail,
                    Status = book.Status,
                    Category = new CategoryDTO { Id = book.CategoryId },
                }
            );
        }

        [HttpGet("{id}/related")]
        public async Task<ActionResult<IEnumerable<BookListResponseDTO>>> GetRelatedBooks(
            int id,
            [FromQuery] int limit = 4
        )
        {
            var currentBook = await _context.Books.FindAsync(id);
            if (currentBook == null)
                return NotFound();

            var relatedBooks = await _context
                .Books.Include(b => b.Category)
                .Include(b => b.User)
                .Where(b => b.Status == 1 && b.CategoryId == currentBook.CategoryId && b.Id != id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit)
                .Select(b => new BookListResponseDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Thumbnail = b.Thumbnail,
                    Summary = b.Summary,
                    ViewCount = b.ViewCount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    FavoriteCount = b.Favorites!.Count,
                    Category = new CategoryDTO { Id = b.Category!.Id, Name = b.Category.Name },
                    User = new UserDTO { Id = b.User!.Id, FullName = b.User.FullName },
                })
                .ToListAsync();

            return Ok(relatedBooks);
        }

        [Authorize(Roles = "user,admin")]
        [HttpGet("my-books")]
        public async Task<ActionResult<PagedResponse<IEnumerable<BookListResponseDTO>>>> GetMyBooks(
            [FromQuery] MyBookFilter filter
        )
        {
            var currentUserId = GetCurrentUserId()!.Value;
            var query = _context
                .Books.Include(b => b.Category)
                .Include(b => b.Favorites)
                .Where(b =>
                    b.UserId == currentUserId && b.Category != null && !b.Category.IsDeleted
                )
                .AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(b => b.Status == filter.Status.Value);

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BookListResponseDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Thumbnail = b.Thumbnail,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    ViewCount = b.ViewCount,
                    FavoriteCount = b.Favorites.Count,
                    Category = new CategoryDTO { Id = b.Category!.Id, Name = b.Category.Name },
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

        [HttpPost("{id}/increment-view")]
        public async Task<IActionResult> IncrementViewCount(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();
            book.ViewCount += 1;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã tăng lượt xem", newViewCount = book.ViewCount });
        }

        // POST: api/Books
        [Authorize(Roles = "admin,user")]
        [HttpPost]
        public async Task<IActionResult> PostBook([FromForm] CreateUpdateBookDTO request)
        {
            var currentUserId = GetCurrentUserId()!.Value;

            var isAdmin = User.IsInRole("admin");

            var book = new Book
            {
                Title = request.Title,
                Slug = StringHelper.GenerateSlug(request.Title),
                Summary = request.Summary,
                Content = request.Content,
                CategoryId = request.CategoryId,
                Thumbnail = await FileHelper.UploadFileAsync(request.ThumbnailFile, "books"),
                UserId = currentUserId,
                CreatedAt = DateTime.Now,
                Status = isAdmin ? (byte)1 : (byte)0,
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            string responseMessage = isAdmin
                ? "Đăng bài thành công! Bài viết đã được hiển thị."
                : "Bài viết đã lưu và đang chờ Admin phê duyệt.";

            return Ok(new { success = true, message = responseMessage });
        }

        // PUT: api/Books/{id}
        [Authorize(Roles = "admin,user")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, [FromForm] CreateUpdateBookDTO request)
        {
            var oldBook = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (oldBook == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId()!.Value;
            var isAdmin = User.IsInRole("admin");

            if (!isAdmin && oldBook.UserId != currentUserId)
            {
                return Forbid();
            }

            if (!isAdmin && oldBook.Status == 3)
            {
                return BadRequest(
                    new { message = "Bài viết này đang bị khóa, bạn không thể chỉnh sửa nội dung." }
                );
            }

            _context.BookHistories.Add(
                new BookHistory
                {
                    BookId = id,
                    OldContent = oldBook.Content,
                    EditedByUserId = currentUserId,
                    CreatedAt = DateTime.Now,
                }
            );

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
                Status = isAdmin ? oldBook.Status : (byte)0,
            };

            _context.Entry(updatedBook).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            string responseMessage = isAdmin
                ? "Cập nhật bài viết thành công!"
                : "Cập nhật thành công, đang chờ duyệt lại bài viết!";

            return Ok(new { success = true, message = responseMessage });
        }

        // DELETE: api/Books/{id}
        [Authorize(Roles = "admin,user")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            var currentUserId = GetCurrentUserId()!.Value;
            if (book.UserId != currentUserId)
                return Forbid();

            book.IsDeleted = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa bài viết." });
        }

        // PUT: api/Books/{id}/toggle-visibility
        [Authorize(Roles = "user,admin")]
        [HttpPut("{id}/toggle-visibility")]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            var currentUserId = GetCurrentUserId()!.Value;
            var isAdmin = User.IsInRole("admin");

            if (!isAdmin && book.UserId != currentUserId)
                return Forbid();

            if (book.Status == 0)
                return BadRequest(
                    new { message = "Bài viết đang chờ duyệt, không thể thay đổi hiển thị." }
                );
            if (book.Status == 3)
                return BadRequest(
                    new { message = "Bài viết này đã bị Admin khóa, không thể tự mở lại." }
                );

            book.Status = book.Status == 1 ? (byte)2 : (byte)1;
            book.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            string responseMsg =
                book.Status == 1
                    ? "Đã hiển thị bài viết công khai."
                    : "Đã chuyển bài viết về chế độ riêng tư.";
            return Ok(
                new
                {
                    success = true,
                    message = responseMsg,
                    newStatus = book.Status,
                }
            );
        }

        [HttpGet("user/{username}")]
        public async Task<
            ActionResult<PagedResponse<IEnumerable<BookListResponseDTO>>>
        > GetUserBooks(string username, [FromQuery] PaginationFilter filter)
        {
            var query = _context
                .Books.Include(b => b.Category)
                .Include(b => b.User)
                .Include(b => b.Favorites)
                .Where(b =>
                    b.User!.Username == username
                    && b.Status == 1
                    && b.Category != null
                    && !b.Category.IsDeleted
                )
                .AsQueryable();

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BookListResponseDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Thumbnail = b.Thumbnail,
                    Summary = b.Summary,
                    ViewCount = b.ViewCount,
                    CreatedAt = b.CreatedAt,
                    FavoriteCount = b.Favorites.Count,
                    Category = new CategoryDTO { Name = b.Category!.Name },
                    User = new UserDTO { FullName = b.User!.FullName, Username = b.User.Username },
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

        // GET: api/Books/{id}/history
        [Authorize(Roles = "user,admin")]
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<BookHistoryDTO>>> GetMyBookHistory(int id)
        {
            var currentUserId = GetCurrentUserId()!.Value;
            var isAdmin = User.IsInRole("admin");

            var book = await _context
                .Books.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return NotFound(new { message = "Không tìm thấy bài viết." });

            if (!isAdmin && book.UserId != currentUserId)
                return Forbid();

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
