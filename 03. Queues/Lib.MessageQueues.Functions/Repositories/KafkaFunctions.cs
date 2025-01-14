using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Lib.MessageQueues.Functions.Repositories
{
    public class KafkaFunctions : IKafkaFunctions
    {
        private IProducer<Null, string> producer;
        private IConsumer<Null, string> consumer;
        private string bootstrapServers = "localhost:9092";

        public KafkaFunctions()
        {
            // Crear el productor de Kafka
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            producer = new ProducerBuilder<Null, string>(config).Build();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = "test-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = true
            };
            consumer = new ConsumerBuilder<Null, string>(consumerConfig).Build();
        }

        #region Public Methods

        public async Task CreateQueueAsync(string topicName, int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey)
        {
            var config = new AdminClientConfig { BootstrapServers = bootstrapServers };
            using (var adminClient = new AdminClientBuilder(config).Build())
            {
                try
                {
                    var topicSpecification = new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = 3, // Número de particiones
                        ReplicationFactor = 1 // Número de réplicas
                    };

                    // Crear el topic
                    await adminClient.CreateTopicsAsync(new List<TopicSpecification> { topicSpecification });
                    Console.WriteLine($"Topic '{topicName}' creado exitosamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al crear el topic: {ex.Message}");
                }
            }
        }

        public async Task DeleteQueues()
        {

        }

        public async Task EmitMessagePending(string topicName, Guid attentionId, Guid patientId, DateTime birthday, int? comorbidities, int planRecord, Guid cityId, Guid processId)
        {
            var message = new MessageInfo
            {
                Id = attentionId.ToString(),
                PatientId = patientId.ToString(),
                CityId = cityId.ToString(),
                ProcessId = processId.ToString()
            };

            string jsonMessage = JsonConvert.SerializeObject(message);

            try
            {
                await producer.ProduceAsync(topicName, new Message<Null, string> { Value = jsonMessage });
                Console.WriteLine($"Mensaje emitido a {topicName}");
            }
            catch (ProduceException<Null, string> e)
            {
                Console.WriteLine($"Error al emitir mensaje: {e.Error.Reason}");
            }
        }



        public async Task EmitGenericMessage(Guid messageId, string topicNameOrigin, string topicNameTarget)
        {
            await MoveMessageQueue(messageId, topicNameOrigin, topicNameTarget);
        }
        public async Task<string> EmitMessageAsign(string queueNameAsign, string queueNamePend, Guid HealthCareStaffId)
        {
            MessageInfo messageInfo = new();

            // Consumiendo el primer mensaje de la cola "Pendiente"
            var consumeResult = await ConsumeMessageAsync(queueNamePend);
            if (consumeResult != null)
            {
                // Procesamos el mensaje recibido
                messageInfo = BuildMessageAsign(consumeResult, HealthCareStaffId.ToString());
                messageInfo.HealthCareStaffId = HealthCareStaffId.ToString();

                // Enviar el mensaje a la cola "Asignada"
                await SendMessageAsync(queueNameAsign, messageInfo);

                // Retornar el ID del mensaje
                return messageInfo.Id;
            }

            return string.Empty;
        }
        #endregion

        #region Private Methods

        private async Task MoveMessageQueue(Guid messageId, string originTopic, string targetTopic)
        {
            consumer.Subscribe(originTopic);

            bool foundMessage = false;
            try
            {
                while (!foundMessage)
                {
                    var consumeResult = consumer.Consume(CancellationToken.None);
                    var message = consumeResult.Message.Value;
                    var objMessage = JsonConvert.DeserializeObject<MessageInfo>(message);

                    if (objMessage.Id == messageId.ToString())
                    {
                        // Mover el mensaje al nuevo topic
                        await producer.ProduceAsync(targetTopic, new Message<Null, string> { Value = consumeResult.Message.Value });
                        consumer.Commit(consumeResult);
                        foundMessage = true;
                        Console.WriteLine($"Mensaje movido de {originTopic} a {targetTopic}");
                    }
                }
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Error al consumir el mensaje: {e.Error.Reason}");
            }
        }
        private async Task<ConsumeResult<Null, string>> ConsumeMessageAsync(string queueName)
        {
            consumer.Subscribe(queueName);  // En Kafka, "queueName" sería un "topic"

            // Esperamos hasta que haya un mensaje disponible en el "topic"
            var consumeResult = await Task.Run(() => consumer.Consume(CancellationToken.None));

            return consumeResult;
        }

        private async Task SendMessageAsync(string queueName, MessageInfo messageInfo)
        {
            var message = JsonConvert.SerializeObject(messageInfo);

            // Enviar el mensaje al productor de Kafka (enviar al "topic" de Kafka)
            await Task.Run(() =>
            {
                producer.Produce(queueName, new Message<Null, string> { Value = message });
            });
        }

        private MessageInfo BuildMessageAsign(ConsumeResult<Null, string> consumeResult, string medicId)
        {
            var mensaje = consumeResult.Message.Value;
            var messageInfo = JsonConvert.DeserializeObject<MessageInfo>(mensaje);
            messageInfo.HealthCareStaffId = medicId;
            return messageInfo;
        }
        #endregion

        #region Helper Methods

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

        #endregion

        public void Close()
        {
            producer?.Dispose();
            consumer?.Close();
            consumer?.Dispose();
            Console.WriteLine("Conexión a Kafka cerrada.");
        }

        public Task CreateQueueAsync(string queueName, bool? durable, bool? exclusive, bool? autoDelete, int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey)
        {
            throw new NotImplementedException();
        }
    }
}
