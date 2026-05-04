using MailKit.Net.Smtp;
using MimeKit;

namespace ViNgocHiep_2123110365.Helpers
{
    public class EmailHelper
    {
        public static async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();

                var senderEmail = "duchiep.officials@gmail.com";
                var appPassword = "kumbabjsvhyilyqx";

                message.From.Add(new MailboxAddress("VastVerse System", senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        "smtp.gmail.com",
                        587,
                        MailKit.Security.SecureSocketOptions.StartTls
                    );

                    await client.AuthenticateAsync(senderEmail, appPassword);

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                return false;
            }
        }
    }
}
