
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using Web.Core.Business.API.DTOs.Input;

namespace Lib.MessageQueues.Functions.Repositories.RabbitMQ
{

    public class RabbitMQPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly RabbitMQSettingDTO _rabbitMqSetting;

        public RabbitMQPublisher(IOptions<RabbitMQSettingDTO> rabbitMQSettings)
        {
            _rabbitMqSetting = rabbitMQSettings.Value;
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqSetting.HostName,
                UserName = _rabbitMqSetting.UserName,
                Password = _rabbitMqSetting.Password
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ConfirmSelect();
        }

        public async Task<(bool success, string message)> PublishMessageAsync(string queueName, string message, byte Priority)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; 
                properties.Priority = Priority; 

                await Task.Run(() => _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body));
                _channel.WaitForConfirmsOrDie(); 

                return (true, "Mensaje publicado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al publicar el mensaje: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }

}

