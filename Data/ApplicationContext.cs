using Microsoft.EntityFrameworkCore;
using owobot_csharp.Models;

namespace owobot_csharp.Data;

public class ApplicationContext : DbContext
{
    public ApplicationContext()
    {
            
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"DataSource={Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName}/Database.db");
        base.OnConfiguring(optionsBuilder);
    }
    
    public DbSet<User> Users { get; set; }
}