using System.ComponentModel.DataAnnotations.Schema;

namespace ViNgocHiep_2123110365.Models;

public class Follow
{
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    [ForeignKey("FollowerId")]
    public User? Follower { get; set; }

    [ForeignKey("FollowingId")]
    public User? Following { get; set; }
}
