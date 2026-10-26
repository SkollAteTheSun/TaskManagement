using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagement.Configuration;
using TaskManagement.DTO;

namespace TaskManagement.Services;


// HACK: упрощённая реализация фонового отправителя
// создаём соединение для каждого события, чтобы код было проще проверить
// в дальнейшем вынес бы соединение и канал на уровень BackgroundService
// для повторного использования и добавил обработку переподключений
public class RabbitMqBackgroundService : BackgroundService
{
    private readonly TaskEventQueue _queue;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqBackgroundService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public RabbitMqBackgroundService(TaskEventQueue queue, ILogger<RabbitMqBackgroundService> logger, IOptions<RabbitMqOptions> options)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await PublishAsync(message, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish task.completed event for {TaskId}",
                        message.TaskId);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Приложение останавливается.
        }
    }

    private async Task PublishAsync(TaskCompletedEvent message, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange: _options.ExchangeName, type: ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync( exchange: _options.ExchangeName, routingKey: _options.RoutingKey, mandatory: false,
            basicProperties: properties, body: body, cancellationToken: cancellationToken);

        _logger.LogInformation( "Published task.completed event for {TaskId}", message.TaskId);
    }


    // HACK: Graceful shutdown реализован частично
    // при остановке завершаем очередь и освобождаем ресурсы через await using
    // но BackgroundService.StopAsync() сразу отменяет stoppingToken.
    // поэтому публикации могут прерваться, не дождавшись завершения.
    // доработал бы ожидание активных публикаций с таймаутом 3–5 секунд.
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Complete();
        await base.StopAsync(cancellationToken);
    }
}