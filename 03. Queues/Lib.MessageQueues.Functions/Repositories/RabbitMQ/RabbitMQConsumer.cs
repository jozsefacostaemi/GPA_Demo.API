
//using Microsoft.Extensions.Options;
//using RabbitMQ.Client;
//using System;
//using System.Text;
//using System.Threading.Tasks;
//using Web.Core.Business.API.DTOs.Input;

//namespace Lib.MessageQueues.Functions.Repositories.RabbitMQ
//{

//    public class RabbitMQConsumer : IDisposable
//    {
//        private readonly IConnection _connection;
//        private readonly IModel _channel;
//        private readonly RabbitMQSettingDTO _rabbitMqSetting;

//        public RabbitMQConsumer(IOptions<RabbitMQSettingDTO> rabbitMQSettings)
//        {
//            _rabbitMqSetting = rabbitMQSettings.Value;
//            var factory = new ConnectionFactory
//            {
//                HostName = _rabbitMqSetting.HostName,
//                UserName = _rabbitMqSetting.UserName,
//                Password = _rabbitMqSetting.Password
//            };
//            _connection = factory.CreateConnection();
//            _channel = _connection.CreateModel();
//            _channel.BasicQos(0, 1, false); // Set prefetch count to 1
//        }


//        public async Task<(bool success, string message, ulong deliveryTag)> ConsumeMessageAsync(string queueName)
//        {
//            var result = await Task.Run(() => _channel.BasicGet(queueName, autoAck: false));
//            if (result == null)
//            {
//                return (false, "No hay mensajes en la cola", 0);
//            }

//            var message = Encoding.UTF8.GetString(result.Body.ToArray());
//            return (true, message, result.DeliveryTag);
//        }

//        public async Task<(bool success, string message)> AcknowledgeMessageAsync(ulong deliveryTag)
//        {
//            try
//            {
//                await Task.Run(() => _channel.BasicAck(deliveryTag, multiple: false));
//                return (true, "Mensaje confirmado exitosamente");
//            }
//            catch (Exception ex)
//            {
//                return (false, $"Error al confirmar el mensaje: {ex.Message}");
//            }
//        }

//        public async Task<(bool success, string message)> NacknowledgeMessageAsync(ulong deliveryTag)
//        {
//            try
//            {
//                await Task.Run(() => _channel.BasicNack(deliveryTag, multiple: false, requeue: true));
//                return (true, "Mensaje confirmado exitosamente");
//            }
//            catch (Exception ex)
//            {
//                return (false, $"Error al confirmar el mensaje: {ex.Message}");
//            }
//        }

//        public void Dispose()
//        {
//            _channel?.Close();
//            _connection?.Close();
//        }
//    }
//}