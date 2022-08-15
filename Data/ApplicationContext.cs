﻿using Microsoft.EntityFrameworkCore;
using owobot_csharp.Models;

namespace owobot_csharp.Data;

public class ApplicationContext : DbContext
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("DataSource=Essentials/Database.db");
        /*optionsBuilder.UseSqlite(
            $"DataSource={Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName}/Database.db");*/
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Chat> Chats { get; set; } = default!;
}