using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class PersonState
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool Active { get; set; }

    public bool? IsHealthCareStaff { get; set; }

    public bool? IsPatient { get; set; }

    public virtual ICollection<HealthCareStaff> HealthCareStaffs { get; set; } = new List<HealthCareStaff>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
