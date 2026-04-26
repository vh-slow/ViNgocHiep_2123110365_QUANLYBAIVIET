using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;
using ViNgocHiep_2123110365.Helpers;

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

            return Ok(
                new UserProfileDTO
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Avatar = user.Avatar,
                    Bio = user.Bio,
                    Role = user.Role,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                }
            );
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
    }
}
