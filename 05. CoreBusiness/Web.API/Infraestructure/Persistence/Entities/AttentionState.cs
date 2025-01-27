using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class AttentionState
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public bool Active { get; set; }

    public string Color { get; set; } = null!;

    public virtual ICollection<AttentionHistory> AttentionHistories { get; set; } = new List<AttentionHistory>();

    public virtual ICollection<Attention> Attentions { get; set; } = new List<Attention>();

    public virtual ICollection<ConfQueue> ConfQueues { get; set; } = new List<ConfQueue>();
}
