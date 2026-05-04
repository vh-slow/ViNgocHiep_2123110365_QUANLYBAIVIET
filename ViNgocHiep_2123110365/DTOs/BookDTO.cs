using System.ComponentModel.DataAnnotations;

namespace ViNgocHiep_2123110365.DTOs
{
    public class BookListResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Thumbnail { get; set; }
        public string? Summary { get; set; }
        public int ViewCount { get; set; }
        public byte Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsFavorited { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
        public CategoryDTO? Category { get; set; }
        public UserDTO? User { get; set; }

        public int FavoriteCount { get; set; } = 0;

        public List<string> Tags { get; set; } = new();
    }

    public class BookDetailResponseDTO : BookListResponseDTO
    {
        public string Content { get; set; } = string.Empty;
        public List<CommentDTO> Comments { get; set; } = new List<CommentDTO>();
    }

    public class BookHistoryDTO
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string OldContent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int EditedByUserId { get; set; }
        public string EditedByUserName { get; set; } = string.Empty;
    }

    public class CreateUpdateBookDTO
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Slug { get; set; }

        [StringLength(500)]
        public string? Summary { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public IFormFile? ThumbnailFile { get; set; }

        public List<string>? TagNames { get; set; }
    }
}
