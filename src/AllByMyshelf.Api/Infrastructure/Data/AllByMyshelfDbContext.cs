using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Infrastructure.Data;

public class AllByMyshelfDbContext(DbContextOptions<AllByMyshelfDbContext> options) 
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AllByMyshelfDbContext).Assembly);
    }
}
