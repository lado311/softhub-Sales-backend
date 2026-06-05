using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SoftHub.API.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Smtp:FromName"] ?? "SoftHub Sales",
            _config["Smtp:From"]!));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "პაროლის აღდგენა — SoftHub";

        message.Body = new TextPart("html")
        {
            Text = $"""
                <div style="font-family:Arial,sans-serif;max-width:520px;margin:0 auto">
                  <h2 style="color:#1a1a2e">პაროლის აღდგენა</h2>
                  <p>გამარჯობა, {toName}!</p>
                  <p>მივიღეთ პაროლის აღდგენის მოთხოვნა თქვენი ანგარიშისთვის.</p>
                  <p>დააჭირეთ ქვემოთ მოცემულ ღილაკს პაროლის შესაცვლელად:</p>
                  <a href="{resetLink}"
                     style="display:inline-block;padding:12px 24px;background:#4f46e5;color:#fff;
                            text-decoration:none;border-radius:6px;margin:16px 0">
                    პაროლის შეცვლა
                  </a>
                  <p style="color:#666;font-size:13px">ლინკი მოქმედებს 1 საათის განმავლობაში.</p>
                  <p style="color:#666;font-size:13px">
                    თუ ეს მოთხოვნა თქვენ არ გაგზავნეთ, უგულებელყავით ეს წერილი.
                  </p>
                  <hr style="border:none;border-top:1px solid #eee;margin:24px 0"/>
                  <p style="color:#999;font-size:12px">SoftHub Sales CRM</p>
                </div>
                """
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Smtp:Host"]!,
            int.Parse(_config["Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Smtp:Username"]!, _config["Smtp:Password"]!);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
