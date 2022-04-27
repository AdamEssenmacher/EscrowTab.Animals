using Microsoft.EntityFrameworkCore;

namespace EscrowTab.Animals;

internal class AnimalContext : DbContext
{
    public const string DbPath = "database.db";
    public DbSet<Animal> Animals => Set<Animal>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
    }
}