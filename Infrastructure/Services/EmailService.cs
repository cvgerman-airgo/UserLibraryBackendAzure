using Application.Interfaces;
using Microsoft.Extensions.Configuration;

using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var smtpConfig = _configuration.GetSection("Email:Smtp");

        var host = smtpConfig["Host"];
        var portString = smtpConfig["Port"];
        var user = smtpConfig["User"];
        var password = smtpConfig["Password"];
        var senderEmail = smtpConfig["SenderEmail"];
        var senderName = smtpConfig["SenderName"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) ||
            string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(senderEmail))
            throw new InvalidOperationException("Faltan datos de configuración SMTP en appsettings.json.");

        var port = int.Parse(portString);

        using var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(to);

        try
        {
            await smtp.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Aquí podrías usar un logger si tienes inyección de ILogger<EmailService>
            throw new InvalidOperationException("Error enviando el correo electrónico.", ex);
        }
    }
}
