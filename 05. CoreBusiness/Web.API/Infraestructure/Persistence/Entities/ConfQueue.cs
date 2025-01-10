using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class ConfQueue
{
    public Guid Id { get; set; }

    public Guid? CityId { get; set; }

    public Guid? ProcessId { get; set; }

    public Guid? AttentionStateId { get; set; }

    public bool? Durable { get; set; }

    public bool? AutoDelete { get; set; }

    public int? Nprocessor { get; set; }

    public bool? Active { get; set; }

    public int? NOrder { get; set; }

    public int? MaxPriority { get; set; }

    public bool? Exclusive { get; set; }

    public int? MessageLifeTime { get; set; }

    public int? QueueExpireTime { get; set; }

    public string? QueueMode { get; set; }

    public string? QueueDeadLetterExchange { get; set; }

    public string? QueueDeadLetterExchangeRoutingKey { get; set; }

    public virtual AttentionState? AttentionState { get; set; }

    public virtual City? City { get; set; }

    public virtual ICollection<GeneratedQueue> GeneratedQueues { get; set; } = new List<GeneratedQueue>();

    public virtual Processor? Process { get; set; }
}
