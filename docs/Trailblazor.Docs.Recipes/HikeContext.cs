using Microsoft.EntityFrameworkCore;

namespace Trailblazor.Docs.Recipes;

public sealed class Hike
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public required string Trail { get; set; }

    public double Hours { get; set; }
}

public sealed class HikeContext(DbContextOptions<HikeContext> options) : DbContext(options)
{
    public DbSet<Hike> Hikes => Set<Hike>();
}
