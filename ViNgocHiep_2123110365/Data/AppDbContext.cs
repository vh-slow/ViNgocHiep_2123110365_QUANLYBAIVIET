using Microsoft.EntityFrameworkCore;
using ViNgocHiep_2123110365.Models;

namespace ViNgocHiep_2123110365.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<BookHistory> BookHistories { get; set; }

    public DbSet<Tag> Tags { get; set; }
    public DbSet<BookTag> BookTags { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Favorite>().HasKey(f => new { f.UserId, f.BookId });
        modelBuilder.Entity<BookTag>().HasKey(bt => new { bt.BookId, bt.TagId });
        modelBuilder.Entity<Follow>().HasKey(f => new { f.FollowerId, f.FollowingId });

        modelBuilder
            .Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .Entity<Favorite>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Followings)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Book>().HasQueryFilter(b => !b.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted && !c.Book!.IsDeleted);
        modelBuilder.Entity<Favorite>().HasQueryFilter(f => !f.Book!.IsDeleted);
        modelBuilder.Entity<BookHistory>().HasQueryFilter(h => !h.Book!.IsDeleted);

        modelBuilder.Entity<BookTag>().HasQueryFilter(bt => !bt.Book!.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.User!.IsDeleted);
        modelBuilder
            .Entity<Follow>()
            .HasQueryFilter(f => !f.Follower!.IsDeleted && !f.Following!.IsDeleted);
    }
}
