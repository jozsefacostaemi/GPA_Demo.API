using Lib.MessageQueues.Functions.IRepositories;

public class MessagingFunctionsFactory
{
    private readonly IRabbitMQFunctions _rabbitMQFunctions;
    private readonly IKafkaFunctions _kafkaFunctions;

    public MessagingFunctionsFactory(IRabbitMQFunctions rabbitMQFunctions, IKafkaFunctions kafkaFunctions)
    {
        _rabbitMQFunctions = rabbitMQFunctions;
        _kafkaFunctions = kafkaFunctions;
    }

    public IMessagingFunctions GetMessagingFunctions()
    {
        var messagingSystem = MessagingConfiguration.Settings.MessagingSystem;

        return messagingSystem switch
        {
            "RabbitMQ" => _rabbitMQFunctions,
            "Kafka" => _kafkaFunctions,
            _ => throw new InvalidOperationException("Invalid messaging system configuration")
        };
    }
}
