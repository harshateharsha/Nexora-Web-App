using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Nexora.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendOtpAsync(string toEmail, string code, string purpose = "Nexora OTP")
        {
            var smtp = _config.GetSection("Smtp");
            var host = smtp["Host"];
            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var user = smtp["User"];
            var pass = smtp["Password"];
            var from = smtp["From"] ?? "Nexora <no-reply@nexora.app>";

            // Build message
            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = purpose;
            msg.Body = new BodyBuilder
            {
                HtmlBody = $"<p>Your Nexora verification code is: <b>{code}</b></p><p>It expires in 10 minutes.</p>"
            }.ToMessageBody();

            // If SMTP settings missing, fall back to console (dev)
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine($"[DEV SMTP] OTP for {toEmail}: {code}");
                return;
            }

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
    }
}
