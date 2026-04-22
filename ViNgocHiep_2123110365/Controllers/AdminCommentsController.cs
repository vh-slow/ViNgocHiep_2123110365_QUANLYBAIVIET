using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;
using ViNgocHiep_2123110365.Helpers;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api/admin/comments")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminCommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminCommentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/comments
        [HttpGet]
        public async Task<ActionResult<PagedResponse<IEnumerable<CommentDTO>>>> GetComments(
            [FromQuery] AdminCommentFilter filter
        )
        {
            var query = _context
                .Comments.IgnoreQueryFilters()
                .Include(c => c.User)
                .Include(c => c.Book)
                .AsQueryable();

            if (filter.IsDeleted.HasValue)
                query = query.Where(c => c.IsDeleted == filter.IsDeleted.Value);
            if (filter.BookId.HasValue)
                query = query.Where(c => c.BookId == filter.BookId.Value);
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var search = filter.SearchQuery.ToLower().Trim();
                query = query.Where(c =>
                    c.Content.ToLower().Contains(search)
                    || c.User!.Username.ToLower().Contains(search)
                );
            }

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
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
                    },
                })
                .ToListAsync();

            return Ok(
                new PagedResponse<IEnumerable<CommentDTO>>(
                    pagedData,
                    filter.PageNumber,
                    filter.PageSize,
                    totalRecords
                )
            );
        }

        // DELETE: api/admin/comments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommentByAdmin(int id)
        {
            var comment = await _context
                .Comments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                return NotFound();

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã ẩn bình luận vi phạm." });
        }

        // PUT: api/admin/comments/{id}/restore
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreComment(int id)
        {
            var comment = await _context
                .Comments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                return NotFound();

            comment.IsDeleted = false;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã khôi phục bình luận." });
        }
    }
}
