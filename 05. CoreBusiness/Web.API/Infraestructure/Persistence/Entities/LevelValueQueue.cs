using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class LevelValueQueue
{
    public Guid Id { get; set; }

    public Guid LevelId { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? CityId { get; set; }

    public Guid ProcessId { get; set; }

    //public virtual ICollection<BusinessLineLevelValueQueuesConf> BusinessLineLevelValueQueuesConfs { get; set; } = new List<BusinessLineLevelValueQueuesConf>();

    public virtual City? City { get; set; }

    public virtual Country? Country { get; set; }

    public virtual Department? Department { get; set; }

    public virtual LevelQueue Level { get; set; } = null!;

    public virtual Processor Process { get; set; } = null!;
}
