using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;
using ViNgocHiep_2123110365.Helpers;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet]
        public async Task<ActionResult<PagedResponse<IEnumerable<UserProfileDTO>>>> GetUsers(
            [FromQuery] AdminUserFilter filter
        )
        {
            var query = _context.Users.AsQueryable();

            if (filter.Status.HasValue)
                query = query.Where(u => u.Status == filter.Status.Value);
            if (!string.IsNullOrWhiteSpace(filter.Role))
                query = query.Where(u => u.Role == filter.Role);
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var search = filter.SearchQuery.ToLower().Trim();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(search)
                    || u.FullName.ToLower().Contains(search)
                    || u.Email.ToLower().Contains(search)
                );
            }

            var totalRecords = await query.CountAsync();
            var pagedData = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new UserProfileDTO
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Username = u.Username,
                    Avatar = u.Avatar,
                    Bio = u.Bio,
                    Role = u.Role,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                })
                .ToListAsync();

            return Ok(
                new PagedResponse<IEnumerable<UserProfileDTO>>(
                    pagedData,
                    filter.PageNumber,
                    filter.PageSize,
                    totalRecords
                )
            );
        }

        // PUT: api/admin/users/{id}/lock
        [HttpPut("{id}/lock")]
        public async Task<IActionResult> LockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });
            if (user.Role == "admin")
                return BadRequest(new { message = "Không thể khóa tài khoản Admin khác." });

            user.Status = 2; // 2: Locked
            user.UpdatedAt = DateTime.Now;

            var userBooks = await _context
                .Books.Where(b => b.UserId == id && b.Status == 1)
                .ToListAsync();
            foreach (var book in userBooks)
            {
                book.Status = 3;
                book.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(
                new
                {
                    success = true,
                    message = $"Đã khóa tài khoản {user.Username} và ẩn {userBooks.Count} bài viết liên quan.",
                }
            );
        }

        // PUT: api/admin/users/{id}/unlock
        [HttpPut("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Status = 1;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"Đã mở khóa tài khoản {user.Username}." });
        }

        // GET: api/admin/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfileDTO>> GetUserDetail(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            return Ok(
                new UserProfileDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    Avatar = user.Avatar,
                    Bio = user.Bio,
                    Role = user.Role,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                }
            );
        }

        // GET: api/admin/users/{id}/books
        [HttpGet("{id}/books")]
        public async Task<
            ActionResult<PagedResponse<IEnumerable<BookListResponseDTO>>>
        > GetUserBooksByAdmin(int id, [FromQuery] PaginationFilter filter)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == id))
                return NotFound(new { message = "Không tìm thấy người dùng." });

            var query = _context
                .Books.IgnoreQueryFilters()
                .Include(b => b.Category)
                .Include(b => b.User)
                .Where(b => b.UserId == id)
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
                    Status = b.Status,
                    ViewCount = b.ViewCount,
                    CreatedAt = b.CreatedAt,
                    IsDeleted = b.IsDeleted,
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
    }
}
