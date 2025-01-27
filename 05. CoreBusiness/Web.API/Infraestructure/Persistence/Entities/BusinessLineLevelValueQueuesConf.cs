using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class BusinessLineLevelValueQueuesConf
{
    public Guid Id { get; set; }

    public Guid LevelValueQueueId { get; set; }

    public Guid BusinessLineId { get; set; }

    public bool Active { get; set; }

    public virtual BusinessLine BusinessLine { get; set; } = null!;

    public virtual ICollection<ConfQueue> ConfQueues { get; set; } = new List<ConfQueue>();

    public virtual LevelValueQueue LevelValueQueue { get; set; } = null!;
}
