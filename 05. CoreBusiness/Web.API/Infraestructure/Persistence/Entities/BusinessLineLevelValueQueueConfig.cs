using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class BusinessLineLevelValueQueueConfig
{
    public Guid Id { get; set; }

    public Guid LevelQueueId { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? CityId { get; set; }

    public Guid ProcessId { get; set; }

    public Guid? BusinessLineId { get; set; }

    public bool Active { get; set; }

    public virtual BusinessLine? BusinessLine { get; set; }

    public virtual City? City { get; set; }

    public virtual Country? Country { get; set; }

    public virtual Department? Department { get; set; }

    public virtual LevelQueue LevelQueue { get; set; } = null!;

    public virtual Processor Process { get; set; } = null!;

    public virtual ICollection<QueueConf> QueueConfs { get; set; } = new List<QueueConf>();
}
