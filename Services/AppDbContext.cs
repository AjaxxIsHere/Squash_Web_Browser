using Microsoft.EntityFrameworkCore;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.Services;


/*
Summary: This code defines the AppDbContext class, which is a subclass of DbContext from Entity Framework Core. It is used to manage the database context for a web browser application. The class includes:

- DbSet properties for each entity type (Setting, Bookmark, History) to enable CRUD operations.
*/
public class AppDbContext : DbContext
{
    private readonly string _dbFile;

    public AppDbContext(string dbFile)
    {
        _dbFile = dbFile;
    }

    public DbSet<Setting> Settings { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<History> History { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbFile}");
    }
}
