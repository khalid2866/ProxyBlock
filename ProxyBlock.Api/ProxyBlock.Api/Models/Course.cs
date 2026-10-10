namespace ProxyBlock.Api.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public List<CourseSection> Sections { get; set; } = new();
}
