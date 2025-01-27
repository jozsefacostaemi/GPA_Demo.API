using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class AttentionHistory
{
    public Guid Id { get; set; }

    public Guid AttentionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Active { get; set; }

    public Guid? AttentionState { get; set; }

    public Guid GeneratedQueueId { get; set; }

    public virtual Attention Attention { get; set; } = null!;

    public virtual AttentionState? AttentionStateNavigation { get; set; }

    public virtual GeneratedQueue GeneratedQueue { get; set; } = null!;
}
