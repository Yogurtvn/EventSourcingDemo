namespace NotificationService.Services;

public static class NotificationDispatchStatus
{
    public const string Sent = "Sent";
    public const string SkippedNoRecipient = "SkippedNoRecipient";
    public const string SkippedNotConfigured = "SkippedNotConfigured";
    public const string Failed = "Failed";
}
