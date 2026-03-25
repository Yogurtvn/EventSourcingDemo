using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotificationService.Services;

public sealed class NotificationEmailService(IConfiguration configuration, ILogger<NotificationEmailService> logger)
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        var fromEmail = configuration["SendGrid:FromEmail"];
        var fromName = configuration["SendGrid:FromName"] ?? "EventSourcingDemo";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
        {
            logger.LogWarning(
                "SendGrid not configured (SendGrid:ApiKey / SendGrid:FromEmail). Would send to {To}: {Subject}",
                toEmail,
                subject);
            return;
        }

        try
        {
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);
            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("SendGrid OK: {Subject} -> {To}", subject, toEmail);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "SendGrid failed: {StatusCode} {Body}",
                    response.StatusCode,
                    responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SendGrid error sending to {To}", toEmail);
            throw;
        }
    }
}
