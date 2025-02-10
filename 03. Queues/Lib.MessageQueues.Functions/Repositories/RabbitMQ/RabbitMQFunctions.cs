using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Data.Common;
using System.Text;
using System.Threading.Channels;
using Web.Core.Business.API.DTOs.Input;

namespace Lib.MessageQueues.Functions.Repositories.RabbitMQ
{
    public class RabbitMQFunctions : IRabbitMQFunctions
    {
        #region Variables
        private readonly ConnectionFactory factory;
        private readonly RabbitMQSettingDTO _rabbitMqSetting;
        #endregion

        #region Ctor
        public RabbitMQFunctions(IOptions<RabbitMQSettingDTO> rabbitMQSettings, RabbitMQPublisher rabbitMQPublisher, RabbitMQConsumer rabbitMQConsumer)
        {
            _rabbitMqSetting = rabbitMQSettings.Value;
            factory = new ConnectionFactory
            {
                HostName = _rabbitMqSetting.HostName,
                UserName = _rabbitMqSetting.UserName,
                Password = _rabbitMqSetting.Password
            };
        }
        #endregion

        #region Public Methods    
        /* Función que crea las colas con base a lo parametrizado en la base de datos */
        public async Task CreateQueueAsync(string queueName, bool? durable, bool? exclusive, bool? autoDelete, int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey)
        {
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                try
                {
                    var arguments = BuildArgumentsDictionary(MaxPriority, MessageLifeTime, QueueExpireTime, QueueMode, QueueDeadLetterExchange, QueueDeadLetterExchangeRoutingKey);
                    await Task.Run(() => channel.QueueDeclare(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: arguments));

                    Console.WriteLine($"Cola '{queueName}' creada exitosamente");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al crear la cola: {ex.Message}");
                }
            }
        }
        /* Función que elimina todas las colas */
        public async Task DeleteQueues()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:15672/api/");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("guest:guest")));

            var response = await client.GetAsync("queues");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var queues = JArray.Parse(jsonResponse);

            var deleteTasks = new List<Task>();

            foreach (var queue in queues)
            {
                string queueName = queue["name"].ToString();
                Console.WriteLine($"Agregando cola para eliminar: {queueName}");

                deleteTasks.Add(DeleteQueueAsync(client, queueName));
            }

            await Task.WhenAll(deleteTasks);

            Console.WriteLine("Todas las colas han sido eliminadas.");
        }

        #endregion

        #region Private Methods
        /* Función que mapea las propiedades de los maestros de colas */
        private Dictionary<string, object> BuildArgumentsDictionary(int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey)
        {
            var arguments = new Dictionary<string, object>();
            if (MaxPriority.HasValue)
                arguments.Add("x-max-priority", MaxPriority.Value);
            if (MessageLifeTime.HasValue)
                arguments.Add("x-message-ttl", MessageLifeTime.Value);
            if (QueueExpireTime.HasValue)
                arguments.Add("x-expires", QueueExpireTime.Value);
            if (!string.IsNullOrEmpty(QueueMode))
                arguments.Add("x-queue-mode", QueueMode);
            if (!string.IsNullOrEmpty(QueueDeadLetterExchange))
                arguments.Add("x-dead-letter-exchange", QueueDeadLetterExchange);
            if (!string.IsNullOrEmpty(QueueDeadLetterExchangeRoutingKey))
                arguments.Add("x-dead-letter-routing-key", QueueDeadLetterExchangeRoutingKey);
            return arguments;
        }
        /* Función que elimina una cola por su nombre */
        private async Task DeleteQueueAsync(HttpClient client, string queueName)
        {
            var deleteResponse = await client.DeleteAsync($"queues/%2F/{queueName}");
            deleteResponse.EnsureSuccessStatusCode();
            Console.WriteLine($"Cola {queueName} eliminada exitosamente.");
        }    
        #endregion
    }
}
