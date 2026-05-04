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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // ================= PUBLIC APIS =================

        // GET: api/users/profile/{username}
        [HttpGet("profile/{username}")]
        public async Task<ActionResult<UserProfileDTO>> GetPublicProfile(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == user.Id);
            var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == user.Id);

            return Ok(
                new
                {
                    user.Id,
                    user.FullName,
                    user.Username,
                    user.Avatar,
                    user.Bio,
                    user.Role,
                    FollowersCount = followersCount,
                    FollowingCount = followingCount,
                }
            );
        }

        // [POST] api/users/follow/{userId}
        [Authorize]
        [HttpPost("follow/{userId}")]
        public async Task<IActionResult> FollowUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == userId)
                return BadRequest("không thể tự theo dõi!");

            var targetUser = await _context.Users.FindAsync(userId);
            if (targetUser == null)
                return NotFound();

            var existingFollow = await _context.Follows.FindAsync(currentUserId, userId);
            if (existingFollow != null)
                return BadRequest("đã theo dõi người dùng này.");

            var follow = new Follow { FollowerId = currentUserId, FollowingId = userId };
            _context.Follows.Add(follow);

            _context.Notifications.Add(
                new Notification
                {
                    UserId = userId,
                    Content = $"Người dùng {User.Identity!.Name} đã bắt đầu theo dõi bạn.",
                    Type = "follow",
                    RedirectUrl = $"/u/{User.Identity!.Name}",
                }
            );

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã theo dõi." });
        }

        [Authorize]
        [HttpDelete("unfollow/{userId}")]
        public async Task<IActionResult> UnfollowUser(int userId)
        {
            var currentUserId = GetCurrentUserId();
            var follow = await _context.Follows.FindAsync(currentUserId, userId);
            if (follow == null)
                return NotFound();

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã bỏ theo dõi." });
        }

        // GET: api/users/me
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDTO>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();
            var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);
            var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == userId);

            return Ok(
                new UserProfileDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Avatar = user.Avatar,
                    Email = user.Email,
                    Bio = user.Bio,
                    Role = user.Role,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    FollowersCount = followersCount,
                    FollowingCount = followingCount,
                }
            );
        }

        // PUT: api/users/me
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateProfileDTO request)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            user.FullName = request.FullName;
            user.Bio = request.Bio;
            user.UpdatedAt = DateTime.Now;

            if (request.AvatarFile != null)
            {
                user.Avatar = await FileHelper.UploadFileAsync(request.AvatarFile, "users");
            }

            _ = await _context.SaveChangesAsync();
            return Ok(
                new
                {
                    success = true,
                    message = "Cập nhật hồ sơ thành công.",
                    avatarUrl = user.Avatar,
                }
            );
        }

        // PUT: api/users/change-password
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        // GET: api/users/stats/{username}
        [HttpGet("stats/{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserStats(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            var publicBooks = _context.Books.Where(b =>
                b.UserId == user.Id && b.Status == 1 && !b.IsDeleted
            );

            var totalPublished = await publicBooks.CountAsync();

            var totalViews = await publicBooks.SumAsync(b => (int?)b.ViewCount) ?? 0;

            var totalFavorites = await _context.Favorites.CountAsync(f =>
                f.Book!.UserId == user.Id && f.Book.Status == 1 && !f.Book.IsDeleted
            );

            return Ok(
                new
                {
                    totalPublished = totalPublished,
                    totalViews = totalViews,
                    totalFavorites = totalFavorites,
                }
            );
        }

        // GET: api/users/{username}/followers
        [HttpGet("{username}/followers")]
        public async Task<IActionResult> GetFollowers(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();

            var followers = await _context
                .Follows.Where(f => f.FollowingId == user.Id)
                .Join(
                    _context.Users,
                    f => f.FollowerId,
                    u => u.Id,
                    (f, u) =>
                        new
                        {
                            u.Id,
                            u.FullName,
                            u.Username,
                            u.Avatar,
                            u.Bio,
                        }
                )
                .ToListAsync();

            return Ok(followers);
        }

        // GET: api/users/{username}/following
        [HttpGet("{username}/following")]
        public async Task<IActionResult> GetFollowing(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();

            var following = await _context
                .Follows.Where(f => f.FollowerId == user.Id)
                .Join(
                    _context.Users,
                    f => f.FollowingId,
                    u => u.Id,
                    (f, u) =>
                        new
                        {
                            u.Id,
                            u.FullName,
                            u.Username,
                            u.Avatar,
                            u.Bio,
                        }
                )
                .ToListAsync();

            return Ok(following);
        }
    }
}
