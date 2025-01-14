using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.MessageQueues.Functions.IRepositories
{
    public interface IMessagingFunctions
    {
        Task CreateQueueAsync(string queueName, bool? durable, bool? exclusive, bool? autoDelete, int? MaxPriority, int? MessageLifeTime, int? QueueExpireTime, string? QueueMode, string? QueueDeadLetterExchange, string? QueueDeadLetterExchangeRoutingKey);
        Task DeleteQueues();
        Task EmitMessagePending(string queueName, Guid attentionId, Guid patientId, DateTime birthday, int? comorbidities, int planRecord, Guid cityId, Guid processId);
        Task<string> EmitMessageAsign(string queueNameAsign, string queueNamePend, Guid HealthCareStaffId);
        Task EmitGenericMessage(Guid id, string queueNameOrigin, string queueNameTarget);
        
    }
}
