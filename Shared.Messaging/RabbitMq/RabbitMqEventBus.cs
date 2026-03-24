using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace Shared.Messaging.RabbitMq;

public sealed class RabbitMqEventBus : IRabbitMqEventBus, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly IConnection _connection;
    private readonly IModel _publisherChannel;

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        _connection = CreateConnectionWithRetry(factory);
        _publisherChannel = _connection.CreateModel();
        _publisherChannel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    private IConnection CreateConnectionWithRetry(ConnectionFactory factory)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.StartupConnectRetryCount; attempt++)
        {
            try
            {
                var connection = factory.CreateConnection();
                _logger.LogInformation(
                    "RabbitMQ connected on attempt {Attempt} to {Host}:{Port}",
                    attempt,
                    _options.HostName,
                    _options.Port);
                return connection;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "RabbitMQ connect failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms",
                    attempt,
                    _options.StartupConnectRetryCount,
                    _options.StartupConnectRetryDelayMs);

                if (attempt < _options.StartupConnectRetryCount)
                {
                    Thread.Sleep(_options.StartupConnectRetryDelayMs);
                }
            }
        }

        throw new InvalidOperationException(
            $"Cannot connect to RabbitMQ at {_options.HostName}:{_options.Port} after {_options.StartupConnectRetryCount} attempts.",
            lastException);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : Event
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, JsonOptions));
        var properties = _publisherChannel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = typeof(TEvent).Name;

        _publisherChannel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: typeof(TEvent).Name,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("EVENT PUBLISHED: {EventType} AggregateId={AggregateId}", typeof(TEvent).Name, @event.AggregateId);
        return Task.CompletedTask;
    }

    public void Subscribe<TEvent>(string queueName, Func<TEvent, Task> handler)
        where TEvent : Event
    {
        var consumerChannel = _connection.CreateModel();
        consumerChannel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        consumerChannel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        consumerChannel.QueueBind(queueName, _options.ExchangeName, routingKey: typeof(TEvent).Name);
        consumerChannel.BasicQos(0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.Received += async (_, ea) =>
        {
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var retry = ReadRetryCount(ea.BasicProperties);

            try
            {
                var message = JsonSerializer.Deserialize<TEvent>(payload, JsonOptions);
                if (message is null)
                {
                    throw new InvalidOperationException($"Cannot deserialize {typeof(TEvent).Name}");
                }

                _logger.LogInformation(
                    "EVENT CONSUMED: {EventType} Queue={QueueName} AggregateId={AggregateId} Retry={Retry}",
                    typeof(TEvent).Name,
                    queueName,
                    message.AggregateId,
                    retry);

                await handler(message);
                consumerChannel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType}, attempt {Attempt}", typeof(TEvent).Name, retry + 1);

                if (retry < _options.RetryCount)
                {
                    await Task.Delay(_options.RetryDelayMs);
                    RepublishWithRetryHeader(consumerChannel, ea, retry + 1);
                    consumerChannel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogWarning("Event {EventType} moved to dead-letter behavior (drop after max retry)", typeof(TEvent).Name);
                consumerChannel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        consumerChannel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Subscribed queue {QueueName} to event {EventType}", queueName, typeof(TEvent).Name);
    }

    private void RepublishWithRetryHeader(IModel channel, BasicDeliverEventArgs ea, int retryCount)
    {
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = ea.BasicProperties.ContentType;
        properties.Type = ea.BasicProperties.Type;
        properties.Headers = ea.BasicProperties.Headers is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(ea.BasicProperties.Headers);

        properties.Headers["x-retry-count"] = retryCount;

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: ea.RoutingKey,
            basicProperties: properties,
            body: ea.Body);
    }

    private static int ReadRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers is null)
        {
            return 0;
        }

        if (!properties.Headers.TryGetValue("x-retry-count", out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            int parsed => parsed,
            long parsedLong => (int)parsedLong,
            _ => 0
        };
    }

    public void Dispose()
    {
        _publisherChannel.Dispose();
        _connection.Dispose();
    }
}
