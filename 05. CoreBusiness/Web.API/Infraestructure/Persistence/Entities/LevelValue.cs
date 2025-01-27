using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class LevelValue
{
    public Guid Id { get; set; }

    public Guid LevelId { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? Department { get; set; }

    public Guid? CityId { get; set; }

    public Guid ProcessId { get; set; }

    public Guid? BussinessLine { get; set; }

    public virtual BussinessLine? BussinessLineNavigation { get; set; }

    public virtual City? City { get; set; }

    public virtual ICollection<ConfQueue> ConfQueues { get; set; } = new List<ConfQueue>();

    public virtual Country? Country { get; set; }

    public virtual Department? DepartmentNavigation { get; set; }

    public virtual Level Level { get; set; } = null!;

    public virtual Processor Process { get; set; } = null!;
}
