using Microsoft.EntityFrameworkCore;
using ProxyBlock.Api.Models;

namespace ProxyBlock.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AttendanceSession> Sessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> Records => Set<AttendanceRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
