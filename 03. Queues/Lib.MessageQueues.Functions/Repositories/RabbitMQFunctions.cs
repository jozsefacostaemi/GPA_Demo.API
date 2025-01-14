using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Lib.MessageQueues.Functions.Repositories
{
    public class RabbitMQFunctions : IRabbitMQFunctions
    {
        #region Variables
        private IModel channel;
        private IConnection connection;
        #endregion

        #region Ctor
        public RabbitMQFunctions()
        {
            OpenChannel();
        }
        #endregion

        #region Public Methods 
        /* Función que crea las colas con base a lo parametrizado en la base de datos */
        public async Task CreateQueueAsync(string queueName, bool? durable, bool? exclusive, bool? autoDelete, int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey)
        {
            await Task.Run(() =>
            {
                var arguments = BuildArgumentsDictionary(MaxPriority, MessageLifeTime, QueueExpireTime, QueueMode, QueueDeadLetterExchange, QueueDeadLetterExchangeRoutingKey);
                channel.QueueDeclare(queue: queueName,
                                     durable: durable.Value,
                                     exclusive: exclusive.Value,
                                     autoDelete: autoDelete.Value,
                                     arguments: arguments);
                Console.WriteLine($"Cola {queueName} creada y vinculada correctamente.");
            });
        }
        /* Función que elimina todas las colas */
        public async Task DeleteQueues()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:15672/api/");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("guest:guest")));

            var response = await client.GetAsync("queues");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var queues = JArray.Parse(jsonResponse);
            foreach (var queue in queues)
            {
                string queueName = queue["name"].ToString();
                Console.WriteLine($"Eliminando cola: {queueName}");
                var deleteResponse = await client.DeleteAsync($"queues/%2F/{queueName}");
                deleteResponse.EnsureSuccessStatusCode();

                Console.WriteLine($"Cola {queueName} eliminada exitosamente.");
            }
        }
        /* Función que emite un mensaje pendiente */
        public async Task EmitMessagePending(string queueName, Guid attentionId, Guid patientId, DateTime birthday, int? comorbidities, int PlanRecord, Guid cityId, Guid processId)
        {
            var properties = channel.CreateBasicProperties();
            properties.Priority = (byte)calculatedPriority(birthday, comorbidities, PlanRecord);
            channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: properties,
                                 body: BuildMessagePending(attentionId, patientId, cityId, processId));
        }
        /* Función que emite un mensaje asignado */
        public async Task<string> EmitMessageAsign(string queueNameAsign, string queueNamePend, Guid HealthCareStaffId)
        {
            MessageInfo messageInfo = new();
            var result = channel.BasicGet(queue: queueNamePend, autoAck: false);
            if (result != null)
            {
                //Confirmarmos la lectura del mensaje
                channel.BasicAck(deliveryTag: result.DeliveryTag, multiple: false);

                messageInfo = BuildMessageAsign(result, HealthCareStaffId.ToString());
                messageInfo.HealthCareStaffId = HealthCareStaffId.ToString();
                //Enviamos mensaje a cola de Asignado
                channel.BasicPublish(
                    exchange: "",
                    routingKey: queueNameAsign,
                    basicProperties: null,
                    body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageInfo)));

                return messageInfo.Id;
            }
            return string.Empty;
        }
        /* Función que emite un mensaje generico (En proceso, Finalizado, Cancelado) */
        public async Task EmitGenericMessage(Guid messageId, string queueNameOrigin, string queueNameTarget) => await MoveMessageQueue(messageId, queueNameOrigin, queueNameTarget);
        #endregion

        #region Private Methods
        /* Función que calcula la prioridad del mensaje con base a la edad del paciente, comorbilidades y plan relacionado */
        private int? calculatedPriority(DateTime birthDate, int? comorbidities, int planRecord)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now < birthDate.AddYears(age))
                age--;
            int? priority = comorbidities;
            if (age >= 18 && age < 60)
                priority += 1;
            else
                priority += 2;
            priority += planRecord;
            return priority;
        }
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
        /* Función que construye un mensaje pendiente */
        private byte[] BuildMessagePending(Guid attentionId, Guid patientId, Guid CityId, Guid ProcessId)
        {
            MessageInfo objMessage = new MessageInfo { Id = attentionId.ToString(), PatientId = patientId.ToString(), CityId = CityId.ToString(), ProcessId = ProcessId.ToString() };
            string mensajeJson = JsonConvert.SerializeObject(objMessage);
            return Encoding.UTF8.GetBytes(mensajeJson);
        }
        /* Función que construye un mensaje asignado */
        private MessageInfo BuildMessageAsign(BasicGetResult ea, string medicId)
        {
            var body = ea.Body.ToArray();
            var mensaje = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<MessageInfo>(mensaje);
        }
        /* Función que abre el canal de rabbitmq */
        private void OpenChannel()
        {
            if (connection == null || !connection.IsOpen)
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                connection = factory.CreateConnection();
            }
            if (channel == null || channel.IsClosed)
            {
                channel = connection.CreateModel();
                channel.ExchangeDeclare("citaExchange", "direct", durable: true, autoDelete: false);
            }
        }
        /* Función que cierra el canal de rabbitmq */
        private void CloseChannel()
        {
            try
            {
                if (channel != null && channel.IsOpen)
                {
                    channel.Close();
                    Console.WriteLine("Canal cerrado.");
                }

                if (connection != null && connection.IsOpen)
                {
                    connection.Close();
                    Console.WriteLine("Conexión cerrada.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar el canal: {ex.Message}");
            }
        }
        /* Función que mueve el mensaje de una cola a otra */
        private async Task MoveMessageQueue(Guid Id, string OriginNameQueue, string TargetNameQueue)
        {
            OpenChannel();
            var queueDeclareResult = channel.QueueDeclarePassive(OriginNameQueue);
            int totalMessagesInQueue = (int)queueDeclareResult.MessageCount;
            int counter = 0;
            bool foundMessage = false;

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                counter++;
                var mensajeJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                dynamic objMessage = JsonConvert.DeserializeObject(mensajeJson);

                if (objMessage.Id == Id)
                {
                    channel.BasicPublish(exchange: "", routingKey: TargetNameQueue, basicProperties: null, body: ea.Body);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    foundMessage = true;
                }
            };
            channel.BasicConsume(queue: OriginNameQueue, autoAck: false, consumer: consumer);

            while (!foundMessage && counter < totalMessagesInQueue)
                Thread.Sleep(500);

            if (!foundMessage)
                Console.WriteLine("No se encontró el mensaje después de revisar todos los mensajes en la cola.");

            CloseChannel();
        }
        #endregion
    }
}
