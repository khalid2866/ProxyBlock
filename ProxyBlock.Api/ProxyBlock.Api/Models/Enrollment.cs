namespace ProxyBlock.Api.Models;

public class Enrollment
{
    public int Id { get; set; }
    public int CourseSectionId { get; set; }
    public CourseSection CourseSection { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}
