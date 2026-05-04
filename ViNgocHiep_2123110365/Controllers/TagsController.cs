using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TagsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            var tags = await _context.Tags.ToListAsync();
            return Ok(tags);
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingTags([FromQuery] int limit = 10)
        {
            var tags = await _context
                .Tags.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Slug,
                    PostCount = _context.BookTags.Count(bt =>
                        bt.TagId == t.Id && bt.Book.Status == 1 && !bt.Book.IsDeleted
                    ),
                })
                .Where(t => t.PostCount > 0)
                .OrderByDescending(t => t.PostCount)
                .Take(limit)
                .ToListAsync();
            return Ok(tags);
        }

        [HttpGet("related/{bookId}")]
        public async Task<IActionResult> GetRelatedTags(int bookId, [FromQuery] int limit = 7)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
                return NotFound();

            var relatedTags = await _context
                .BookTags.Include(bt => bt.Tag)
                .Where(bt =>
                    bt.Book.CategoryId == book.CategoryId
                    && bt.BookId != bookId
                    && bt.Book.Status == 1
                    && !bt.Book.IsDeleted
                )
                .Select(bt => new
                {
                    bt.Tag!.Id,
                    bt.Tag.Name,
                    bt.Tag.Slug,
                })
                .Distinct()
                .Take(limit)
                .ToListAsync();

            return Ok(relatedTags);
        }
    }
}
