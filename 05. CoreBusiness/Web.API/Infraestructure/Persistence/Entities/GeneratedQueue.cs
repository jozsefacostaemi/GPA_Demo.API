using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class GeneratedQueue
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public Guid ConfigQueueId { get; set; }

    public bool? Active { get; set; }

    public virtual ICollection<AttentionHistory> AttentionHistories { get; set; } = new List<AttentionHistory>();

    public virtual ConfQueue ConfigQueue { get; set; } = null!;
}
