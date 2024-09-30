using BlastFromThePast.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlastFromThePast.Data;

public class AppDbContext : DbContext
{
    public DbSet<AttachmentItem> Items => Set<AttachmentItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=./data/app.db;");
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Warning);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttachmentItem>()
            .HasKey(k => new { k.GuildId, k.ChannelId, k.MessageId, k.AttachmentId });
    }
}