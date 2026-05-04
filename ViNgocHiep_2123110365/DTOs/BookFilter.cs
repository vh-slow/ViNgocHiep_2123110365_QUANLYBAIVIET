using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ViNgocHiep_2123110365.Helpers;

namespace ViNgocHiep_2123110365.DTOs
{
    public class PublicBookFilter : PaginationFilter
    {
        public int? CategoryId { get; set; }
    }

    public class MyBookFilter : PaginationFilter
    {
        public byte? Status { get; set; }
    }

    public class AdminBookFilter : PaginationFilter
    {
        public int? CategoryId { get; set; }
        public byte? Status { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
