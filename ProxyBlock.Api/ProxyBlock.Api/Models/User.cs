namespace ProxyBlock.Api.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Gender Gender { get; set; }
    public UserRole Role { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public string? PhotoPath { get; set; }
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
