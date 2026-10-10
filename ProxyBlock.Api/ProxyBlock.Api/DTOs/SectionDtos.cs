namespace ProxyBlock.Api.DTOs;

public record CreateSectionRequest(string CourseName, string CourseCode, string SectionName);
public record JoinSectionRequest(string JoinCode);
public record AddStudentRequest(string RollNumber);
