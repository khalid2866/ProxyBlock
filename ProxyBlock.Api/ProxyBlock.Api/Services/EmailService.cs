using System.Net;
using System.Net.Mail;

namespace ProxyBlock.Api.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) => _config = config;

    public async Task SendOtpAsync(string toEmail, string code)
    {
        var s = _config.GetSection("Smtp");
        using var client = new SmtpClient(s["Host"], int.Parse(s["Port"]!))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(s["Username"], s["Password"]),
            Timeout = 120000
        };
        var mail = new MailMessage(s["From"]!, toEmail,
            "ProxyBlock — verify your email",
            $"Your ProxyBlock verification code is: {code}\nIt expires in 10 minutes.");
        await client.SendMailAsync(mail);
    }
}
