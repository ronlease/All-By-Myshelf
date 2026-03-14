using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Infrastructure.Data;

public class AllByMyshelfDbContext(DbContextOptions<AllByMyshelfDbContext> options)
    : DbContext(options)
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    public DbSet<BoardGame> BoardGames => Set<BoardGame>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Release> Releases => Set<Release>();

    public DbSet<WantlistRelease> WantlistReleases => Set<WantlistRelease>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AllByMyshelfDbContext).Assembly);
    }
}
