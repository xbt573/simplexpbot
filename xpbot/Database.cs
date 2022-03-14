using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

public class BotDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }

    private string _dbAccess { get; set; }

    public BotDbContext()
    {
        string? dbAccess = Environment.GetEnvironmentVariable("ACCESS");

        if (dbAccess == null) 
        {
            throw new InvalidDataException("ACCESS variable not found in environment, check GitHub page for help");
        }

        _dbAccess = dbAccess;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql(_dbAccess);
}

public class User
{
    public long id { get; set; }
    public long chatId { get; set; }
    public int xp { get; set; }
    public int level { get; set; }

    public int sumXp { get; set; }
}

public class Group
{
    public long id { get; set; }
    public string lang { get; set; }
}