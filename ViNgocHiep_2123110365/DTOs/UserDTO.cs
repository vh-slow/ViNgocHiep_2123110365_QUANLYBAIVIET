using System.ComponentModel.DataAnnotations;
using ViNgocHiep_2123110365.Helpers;

namespace ViNgocHiep_2123110365.DTOs
{
    public class AdminUserFilter : PaginationFilter
    {
        public byte? Status { get; set; }
        public string? Role { get; set; }
    }

    public class UserProfileDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string Role { get; set; } = string.Empty;
        public byte Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
    }

    public class UpdateProfileDTO
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Bio { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}
