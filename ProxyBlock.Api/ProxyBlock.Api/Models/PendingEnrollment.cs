namespace ProxyBlock.Api.Models;

public class PendingEnrollment
{
    public int Id { get; set; }
    public int CourseSectionId { get; set; }
    public CourseSection CourseSection { get; set; } = null!;
    public string RollNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
