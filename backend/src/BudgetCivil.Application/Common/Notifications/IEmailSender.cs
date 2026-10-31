namespace BudgetCivil.Application.Common.Notifications;

/// <summary>D-17/RF-18: unico canal de correo transaccional del sistema (creacion de cuenta y recuperacion).</summary>
public interface IEmailSender
{
    Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default);
}