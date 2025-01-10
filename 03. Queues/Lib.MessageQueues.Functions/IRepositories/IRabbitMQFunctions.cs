using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.MessageQueues.Functions.IRepositories
{
    public interface IRabbitMQFunctions
    {
        Task CreateQueueAsync(string queueName, bool? durable, bool? exclusive, bool? autoDelete, int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey);
        Task DeleteQueues();
        Task EmitMessagePending(string queueName, Guid attentionId, Guid patientId, DateTime age, int? Comorbities, int planRecord, Guid CityId, Guid processId);
        Task<string> EmitMessageAsign(string queueNameAsign, string queueNamePend, Guid HealthCareStaffId);
        Task EmitMessageInProcess(Guid id, string queueNameAsign, string queueNameInProcess);
        Task EmitMessageFinish(Guid id, string queueNameInProcess, string queueNameFinish);

        
    }
}
