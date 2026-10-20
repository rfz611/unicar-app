namespace UniCar.API.Services;

public interface IEmailSender
{
    Task SendPasswordResetCodeAsync(string toEmail, string toName, string code);
}

/// <summary>
/// Implementação de desenvolvimento: não envia e-mail, apenas escreve o código no console da API.
/// Para produção, troque por uma implementação com SMTP (ex.: MailKit) registrada no Program.cs.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetCodeAsync(string toEmail, string toName, string code)
    {
        _logger.LogWarning("[DEV] Código de redefinição de senha para {Email}: {Code} (válido por 15 minutos)", toEmail, code);
        return Task.CompletedTask;
    }
}
