using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;
using ViNgocHiep_2123110365.Models;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "user,admin")]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: api/Favorites/my-favorites
        [HttpGet("my-favorites")]
        public async Task<ActionResult<IEnumerable<BookListResponseDTO>>> GetMyFavorites()
        {
            var userId = GetCurrentUserId();

            var favoriteBooks = await _context
                .Favorites.Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .ThenInclude(b => b.Category)
                .Include(f => f.Book)
                .ThenInclude(b => b.User)
                .Where(f => f.Book!.IsDeleted == false && f.Book.Status != 3)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new BookListResponseDTO
                {
                    Id = f.Book!.Id,
                    Title = f.Book.Title,
                    Slug = f.Book.Slug,
                    Thumbnail = f.Book.Thumbnail,
                    Summary = f.Book.Summary,
                    ViewCount = f.Book.ViewCount,
                    Status = f.Book.Status,
                    CreatedAt = f.Book.CreatedAt,
                    FavoriteCount = f.Book.Favorites.Count,
                    Category = new CategoryDTO
                    {
                        Id = f.Book.Category!.Id,
                        Name = f.Book.Category.Name,
                        Slug = f.Book.Category.Slug,
                    },
                    User = new UserDTO
                    {
                        Id = f.Book.User!.Id,
                        FullName = f.Book.User.FullName,
                        Username = f.Book.User.Username,
                    },
                })
                .ToListAsync();

            return Ok(favoriteBooks);
        }

        // POST: api/Favorites/Book/{bookId}
        [HttpPost("Book/{bookId}")]
        public async Task<IActionResult> ToggleFavorite(int bookId)
        {
            var userId = GetCurrentUserId();

            var book = await _context.Books.FirstOrDefaultAsync(b =>
                b.Id == bookId && !b.IsDeleted && b.Status == 1
            );
            if (book == null)
                return NotFound(new { message = "Sách không tồn tại hoặc đã bị ẩn." });

            var existingFavorite = await _context.Favorites.FirstOrDefaultAsync(f =>
                f.UserId == userId && f.BookId == bookId
            );

            if (existingFavorite != null)
            {
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                var currentFavCount = await _context.Favorites.CountAsync(f => f.BookId == bookId);
                return Ok(
                    new
                    {
                        success = true,
                        message = "Đã bỏ yêu thích.",
                        isFavorited = false,
                        favoriteCount = currentFavCount,
                    }
                );
            }
            else
            {
                var newFavorite = new Favorite
                {
                    UserId = userId,
                    BookId = bookId,
                    CreatedAt = DateTime.Now,
                };
                _context.Favorites.Add(newFavorite);

                if (book.UserId != userId)
                {
                    var senderName = User.Identity?.Name ?? "Ai đó";

                    _context.Notifications.Add(
                        new Notification
                        {
                            UserId = book.UserId,
                            Content = $"{senderName} đã yêu thích bài viết '{book.Title}' của bạn.",
                            Type = "favorite",
                            RedirectUrl = $"/{book.Slug}",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                        }
                    );
                }

                await _context.SaveChangesAsync();
                var currentFavCount = await _context.Favorites.CountAsync(f => f.BookId == bookId);
                return Ok(
                    new
                    {
                        success = true,
                        message = "Đã thêm vào danh sách yêu thích.",
                        isFavorited = true,
                        favoriteCount = currentFavCount,
                    }
                );
            }
        }
    }
}
