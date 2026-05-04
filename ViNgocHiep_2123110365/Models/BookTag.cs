using System.ComponentModel.DataAnnotations.Schema;

namespace ViNgocHiep_2123110365.Models;

public class BookTag
{
    public int BookId { get; set; }
    public int TagId { get; set; }

    [ForeignKey("BookId")]
    public Book? Book { get; set; }

    [ForeignKey("TagId")]
    public Tag? Tag { get; set; }
}
