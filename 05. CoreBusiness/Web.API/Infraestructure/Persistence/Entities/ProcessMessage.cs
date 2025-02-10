using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class ProcessMessage
{
    public Guid Id { get; set; }

    public string Message { get; set; } = null!;

    public bool? Published { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool? Consumed { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid AttentionId { get; set; }

    public Guid QueueConfId { get; set; }

    public virtual Attention Attention { get; set; } = null!;

    public virtual ICollection<ProcessMessageErrorLog> ProcessMessageErrorLogs { get; set; } = new List<ProcessMessageErrorLog>();

    public virtual QueueConf QueueConf { get; set; } = null!;
}
