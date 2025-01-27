using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class BussinessLine
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<HealthCareStaff> HealthCareStaffs { get; set; } = new List<HealthCareStaff>();

    public virtual ICollection<LevelValue> LevelValues { get; set; } = new List<LevelValue>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
