using System.ComponentModel.DataAnnotations;

namespace ViNgocHiep_2123110365.Models;

public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Property
    public ICollection<BookTag>? BookTags { get; set; }
}
