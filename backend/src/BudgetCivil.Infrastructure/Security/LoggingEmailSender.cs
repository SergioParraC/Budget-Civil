using BudgetCivil.Application.Common.Notifications;
using Microsoft.Extensions.Logging;

namespace BudgetCivil.Infrastructure.Security;

/// <summary>
/// D-45: proveedor transaccional simulado. Registra el mensaje y no abre conexiones de red, de modo que
/// RF-18 puede verificarse en local y demo sin credenciales (D-39).
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Transactional email queued to {Recipient}: {Subject}", recipientEmail, subject);
        return Task.CompletedTask;
    }
}