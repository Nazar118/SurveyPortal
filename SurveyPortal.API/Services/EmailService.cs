using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace SurveyPortal.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var mail = new MailMessage()
            {
                From = new MailAddress(emailSettings["Email"] ?? "test@portal.com", "SurveyPortal Sistem"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail));

            //  E-postaları gönderme, projede bir klasöre kaydet!
            var pickupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestEmails");
            if (!Directory.Exists(pickupDirectory))
            {
                Directory.CreateDirectory(pickupDirectory); 
            }

            using var smtp = new SmtpClient()
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = pickupDirectory
            };

            await smtp.SendMailAsync(mail);
        }
    }
}