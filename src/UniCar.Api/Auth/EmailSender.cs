using System.Net;
using System.Net.Mail;

namespace UniCar.Api.Auth;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string textBody, CancellationToken ct = default);
}

public class SmtpOptions
{
    public const string Section = "Smtp";
    /// <summary>Vazio = modo de desenvolvimento: o e-mail aparece no console da API em vez de ser enviado.</summary>
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "nao-responda@unicar.app";
    public string FromName { get; set; } = "UniCar";
    public bool EnableSsl { get; set; } = true;
}

/// <summary>Desenvolvimento: escreve o e-mail no console para você pegar o código.</summary>
public class ConsoleEmailSender(ILogger<ConsoleEmailSender> log) : IEmailSender
{
    public Task SendAsync(string to, string subject, string textBody, CancellationToken ct = default)
    {
        log.LogWarning("E-MAIL (modo dev) para {To}\nAssunto: {Subject}\n{Body}", to, subject, textBody);
        return Task.CompletedTask;
    }
}

/// <summary>Produção: qualquer provedor SMTP (Resend, SendGrid, Amazon SES, Brevo...).</summary>
public class SmtpEmailSender(SmtpOptions options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string textBody, CancellationToken ct = default)
    {
        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            Credentials = new NetworkCredential(options.User, options.Password)
        };
        using var msg = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };
        msg.To.Add(to);
        await client.SendMailAsync(msg, ct);
    }
}
