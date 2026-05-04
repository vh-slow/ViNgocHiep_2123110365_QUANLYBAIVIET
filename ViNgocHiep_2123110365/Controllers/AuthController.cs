using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;
using ViNgocHiep_2123110365.Helpers;
using ViNgocHiep_2123110365.Models;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Tên đăng nhập đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email đã được sử dụng.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                Password = passwordHash,
                Role = "user",
                CreatedAt = DateTime.Now,
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("login")]
        [EnableRateLimiting("AntiSpam")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u =>
                u.Username == request.Username
            );

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

            if (user.Status == 2)
                return StatusCode(
                    403,
                    new { message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin." }
                );

            var token = GenerateJwtToken(user);
            return Ok(
                new
                {
                    token,
                    user = new UserDTO
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Username = user.Username,
                        Avatar = user.Avatar,
                    },
                }
            );
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("AntiSpam")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Ok(new { message = "Email không tồn tại." });

            var otp = new Random().Next(100000, 999999).ToString();
            user.ResetPasswordToken = otp;
            user.ResetPasswordExpiry = DateTime.Now.AddMinutes(15);

            await _context.SaveChangesAsync();

            string body =
                $"<h3>Mã xác thực đổi mật khẩu VastVerse của bạn là: <b style='color:blue'>{otp}</b></h3><p>Mã này có hiệu lực trong 15 phút.</p>";
            await EmailHelper.SendEmailAsync(user.Email, "Mã xác thực đặt lại mật khẩu", body);

            return Ok(new { message = "Mã xác thực đã được gửi tới email của bạn." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == request.Email && u.ResetPasswordToken == request.Token
            );

            if (user == null || user.ResetPasswordExpiry < DateTime.Now)
                return BadRequest(new { message = "Mã xác thực không đúng hoặc đã hết hạn." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đổi mật khẩu mới thành công!" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
