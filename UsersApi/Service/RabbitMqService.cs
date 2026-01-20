using System.Text;
using System.Text.Json;
using Core.Models;
using RabbitMQ.Client;
using UsersApi.Configs;

namespace UsersApi.Service;

public class RabbitMqService : IRabbitMqService, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqService(RabbitMqSettings settings)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            UserName = settings.User,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
