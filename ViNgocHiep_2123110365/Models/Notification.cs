using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViNgocHiep_2123110365.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string Content { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = "info";

    [StringLength(255)]
    public string? RedirectUrl { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public User? User { get; set; }
}
