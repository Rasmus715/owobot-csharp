using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using owobot_csharp.Models;

namespace owobot_csharp.Data;

public sealed class ApplicationContext : DbContext
{
    private readonly IConfiguration _configuration;
    public ApplicationContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=Database.db");
    }
    

    public DbSet<User> Users { get; set; }
}