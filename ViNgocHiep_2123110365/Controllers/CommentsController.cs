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
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [HttpGet("Book/{bookId}")]
        public async Task<ActionResult<IEnumerable<CommentDTO>>> GetCommentsByBook(int bookId)
        {
            var comments = await _context
                .Comments.Include(c => c.User)
                .Where(c => c.BookId == bookId && !c.IsDeleted && c.User!.Status == 1)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDTO
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
                .ToListAsync();

            return Ok(comments);
        }

        // POST: api/Comments
        [Authorize(Roles = "user,admin")]
        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] CreateCommentDTO request)
        {
            var userId = GetCurrentUserId();

            var book = await _context.Books.FirstOrDefaultAsync(b =>
                b.Id == request.BookId && !b.IsDeleted && b.Status == 1
            );
            if (book == null)
                return BadRequest(new { message = "Sách không tồn tại hoặc đã bị ẩn." });

            var comment = new Comment
            {
                Content = request.Content,
                BookId = request.BookId,
                UserId = userId,
                CreatedAt = DateTime.Now,
            };

            _context.Comments.Add(comment);

            if (book.UserId != userId)
            {
                var senderName = User.Identity?.Name ?? "Ai đó";

                _context.Notifications.Add(
                    new Notification
                    {
                        UserId = book.UserId,
                        Content = $"{senderName} đã bình luận về bài viết '{book.Title}' của bạn.",
                        Type = "comment",
                        RedirectUrl = $"/{book.Slug}",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                    }
                );
            }
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã gửi bình luận." });
        }

        // PUT: api/Comments/{id}
        [Authorize(Roles = "user,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComment(int id, [FromBody] UpdateCommentDTO request)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound();

            if (comment.UserId != GetCurrentUserId())
                return Forbid();

            comment.Content = request.Content;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã cập nhật bình luận." });
        }

        // DELETE: api/Comments/{id}
        [Authorize(Roles = "user,admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserId != GetCurrentUserId())
                return Forbid();

            comment.IsDeleted = true;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa bình luận." });
        }
    }
}
