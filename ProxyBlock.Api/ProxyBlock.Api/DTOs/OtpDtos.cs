namespace ProxyBlock.Api.DTOs;

public class VerifyOtpRequest
{
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
}

public class ResendOtpRequest
{
    public string Email { get; set; } = "";
}
