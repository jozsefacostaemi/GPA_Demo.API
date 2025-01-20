using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class HealthCareStaff
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid CityId { get; set; }

    public Guid ProcessId { get; set; }

    public Guid? PersonStateId { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<Attention> Attentions { get; set; } = new List<Attention>();

    public virtual City City { get; set; } = null!;

    public virtual PersonState? PersonState { get; set; }

    public virtual Processor Process { get; set; } = null!;
}
