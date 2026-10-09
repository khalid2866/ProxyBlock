namespace ProxyBlock.Api.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public AttendanceSession Session { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public DateTime ScannedAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool FaceMatched { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? SyncedAt { get; set; }
}
