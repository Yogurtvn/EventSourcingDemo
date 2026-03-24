namespace Shared.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string ExchangeName { get; init; } = "saga.events";
    public int StartupConnectRetryCount { get; init; } = 30;
    public int StartupConnectRetryDelayMs { get; init; } = 1000;
    public int RetryCount { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 200;
}
