using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;


namespace EekiBooks.Utilities
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var password = _configuration["Email:Password"];
            var username = _configuration["Email:Username"];

            using (var welcomeEmail = new MimeMessage())
            {
                welcomeEmail.From.Add(MailboxAddress.Parse(username));
                welcomeEmail.To.Add(MailboxAddress.Parse(email));
                welcomeEmail.Subject = subject;
                welcomeEmail.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlMessage };

                // send email
                using (var emailclient = new SmtpClient())
                {
                    await emailclient.ConnectAsync("smtp.mail.yahoo.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                    await emailclient.AuthenticateAsync(username, password);
                    await emailclient.SendAsync(welcomeEmail);
                    await emailclient.DisconnectAsync(true);
                }
            }
        }
    }
}
