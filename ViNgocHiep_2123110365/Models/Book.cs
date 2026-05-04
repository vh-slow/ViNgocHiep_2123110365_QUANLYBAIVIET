using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViNgocHiep_2123110365.Models;

public class Book
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Thumbnail { get; set; }

    [StringLength(500)]
    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public int ViewCount { get; set; } = 0;

    public byte Status { get; set; } = 1;

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    public ICollection<Comment>? Comments { get; set; }
    public ICollection<Favorite>? Favorites { get; set; }
    public ICollection<BookTag>? BookTags { get; set; }
}
