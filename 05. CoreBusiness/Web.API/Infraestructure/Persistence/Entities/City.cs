using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class City
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool Active { get; set; }

    public Guid DepartmentId { get; set; }

    public virtual ICollection<BusinessLineLevelValueQueueConfig> BusinessLineLevelValueQueueConfigs { get; set; } = new List<BusinessLineLevelValueQueueConfig>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<HealthCareStaff> HealthCareStaffs { get; set; } = new List<HealthCareStaff>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
