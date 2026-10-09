namespace ProxyBlock.Api.DTOs;

public class MarkAttendanceRequest
{
    public int SessionId { get; set; }
    public string Token { get; set; } = "";
}
