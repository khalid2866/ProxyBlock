using ProxyBlock.Api.Models;

namespace ProxyBlock.Api.DTOs;

public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public Gender Gender { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
}
