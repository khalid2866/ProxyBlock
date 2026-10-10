using Microsoft.EntityFrameworkCore;
using ProxyBlock.Api.Models;

namespace ProxyBlock.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AttendanceSession> Sessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> Records => Set<AttendanceRecord>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> Sections => Set<CourseSection>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<PendingEnrollment> PendingEnrollments => Set<PendingEnrollment>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CourseSection>()
            .HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.CourseSectionId, e.StudentId })
            .IsUnique();
    }
}
