namespace NotificationService.Services;

public enum EmailSendOutcome
{
    Delivered,
    SkippedNotConfigured,
    ProviderRejected
}
