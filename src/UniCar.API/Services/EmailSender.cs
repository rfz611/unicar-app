using System.Net;
using System.Net.Mail;

namespace UniCar.API.Services;

/// <summary>
/// Envia e-mails por SMTP quando a seção "Smtp" do appsettings estiver preenchida.
/// Sem SMTP configurado (desenvolvimento), o e-mail é apenas escrito no console da API,
/// o que permite testar a recuperação de senha sem servidor de e-mail.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task SendPasswordResetCodeAsync(string toEmail, string toName, string code)
        => SendAsync(toEmail, "Redefinição de senha — UniCar",
            $"Olá, {toName}. Seu código para redefinir a senha é {code}. Ele é válido por 15 minutos.");

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP não configurado. E-mail NÃO enviado.\nPara: {To}\nAssunto: {Subject}\n{Body}",
                to, subject, body);
            return;
        }

        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var enableSsl = !string.Equals(_config["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
        var user = _config["Smtp:User"];
        var password = _config["Smtp:Password"];
        var from = _config["Smtp:From"] ?? user ?? "no-reply@unicar.app";

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, password);

        using var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message);
    }
}
