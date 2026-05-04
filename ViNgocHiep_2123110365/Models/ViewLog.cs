using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViNgocHiep_2123110365.Models
{
    [Table("ViewLog")]
    public class ViewLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;

        [ForeignKey("BookId")]
        public Book? Book { get; set; }
    }
}
