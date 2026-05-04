using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Data;
using ViNgocHiep_2123110365.DTOs;

namespace ViNgocHiep_2123110365.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalPosts = await _context.Books.CountAsync(b => b.Status == 1 && !b.IsDeleted);
            var pendingPostsCount = await _context.Books.CountAsync(b =>
                b.Status == 0 && !b.IsDeleted
            );
            var totalViews =
                await _context.Books.Where(b => !b.IsDeleted).SumAsync(b => (int?)b.ViewCount) ?? 0;

            var topAuthors = await _context
                .Users.Select(u => new
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Username = u.Username,
                    Avatar = u.Avatar,
                    Role = u.Role,
                    PostCount = u.Books!.Count(b => b.Status == 1 && !b.IsDeleted),
                })
                .Where(u => u.PostCount > 0)
                .OrderByDescending(u => u.PostCount)
                .Take(5)
                .ToListAsync();

            var trendingPosts = await _context
                .Books.Include(b => b.User)
                .Where(b => b.Status == 1 && !b.IsDeleted)
                .OrderByDescending(b => b.ViewCount)
                .Take(5)
                .Select(b => new
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    Thumbnail = b.Thumbnail,
                    ViewCount = b.ViewCount,
                    FavoriteCount = b.Favorites!.Count,
                    AuthorName = b.User!.FullName,
                })
                .ToListAsync();

            return Ok(
                new
                {
                    Stats = new
                    {
                        TotalUsers = totalUsers,
                        TotalPosts = totalPosts,
                        PendingPosts = pendingPostsCount,
                        TotalViews = totalViews,
                    },
                    TopAuthors = topAuthors,
                    TrendingPosts = trendingPosts,
                }
            );
        }

        [HttpGet("analytics")]
        public async Task<ActionResult<AdminAnalyticsDTO>> GetAnalytics()
        {
            var last7Days = Enumerable
                .Range(0, 7)
                .Select(i => DateTime.Now.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var startDate = last7Days.First();

            var usersData = await _context
                .Users.Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var viewsData = await _context
                .ViewLogs.Where(v => v.ViewedAt >= startDate)
                .GroupBy(v => v.ViewedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var response = new AdminAnalyticsDTO();

            foreach (var day in last7Days)
            {
                string dateStr = day.ToString("dd/MM");
                response.UsersChart.Add(
                    new ChartDataDTO
                    {
                        Date = dateStr,
                        Value = usersData.FirstOrDefault(x => x.Date == day)?.Count ?? 0,
                    }
                );
                response.ViewsChart.Add(
                    new ChartDataDTO
                    {
                        Date = dateStr,
                        Value = viewsData.FirstOrDefault(x => x.Date == day)?.Count ?? 0,
                    }
                );
            }

            return Ok(response);
        }
    }
}
