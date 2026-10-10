namespace ProxyBlock.Api.Models;

public class AttendanceSession
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
    public int? CourseSectionId { get; set; }
    public CourseSection? CourseSection { get; set; }
    public string CourseName { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string CurrentToken { get; set; } = "";
    public DateTime TokenExpiresAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<AttendanceRecord> Records { get; set; } = new();
}
