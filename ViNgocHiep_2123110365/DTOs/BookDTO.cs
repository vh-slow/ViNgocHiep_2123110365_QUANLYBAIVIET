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
        public CategoryDTO? Category { get; set; }
        public UserDTO? User { get; set; }

        public int FavoriteCount { get; set; } = 0;
    }

    public class BookDetailResponseDTO : BookListResponseDTO
    {
        public string Content { get; set; } = string.Empty;
        public List<CommentDTO> Comments { get; set; } = new List<CommentDTO>();
    }
}
