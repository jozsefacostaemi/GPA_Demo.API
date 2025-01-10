using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class Processor
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<ConfQueue> ConfQueues { get; set; } = new List<ConfQueue>();
}
