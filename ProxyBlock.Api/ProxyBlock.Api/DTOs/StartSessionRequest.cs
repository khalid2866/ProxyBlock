namespace ProxyBlock.Api.DTOs;

public class StartSessionRequest
{
    public string CourseName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
