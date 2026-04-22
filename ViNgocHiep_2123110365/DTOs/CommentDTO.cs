using System.ComponentModel.DataAnnotations;
using ViNgocHiep_2123110365.Helpers;

namespace ViNgocHiep_2123110365.DTOs
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int BookId { get; set; }

        public UserDTO? User { get; set; }
    }

    public class AdminCommentFilter : PaginationFilter
    {
        public bool? IsDeleted { get; set; }
        public int? BookId { get; set; }
    }

    public class CreateCommentDTO
    {
        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }
    }

    public class UpdateCommentDTO
    {
        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = string.Empty;
    }
}
