namespace ProxyBlock.Api.Models;

public class CourseSection
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string SectionName { get; set; } = "";
    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
    public List<Enrollment> Enrollments { get; set; } = new();
}
