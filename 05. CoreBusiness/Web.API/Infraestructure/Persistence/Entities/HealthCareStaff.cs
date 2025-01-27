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

    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public Guid Rol { get; set; }

    public string Email { get; set; } = null!;

    public Guid BusinessLineId { get; set; }

    public DateTime? AvailableAt { get; set; }

    public bool Loggued { get; set; }

    public virtual ICollection<Attention> Attentions { get; set; } = new List<Attention>();

    public virtual BusinessLine BusinessLine { get; set; } = null!;

    public virtual City City { get; set; } = null!;

    public virtual PersonState? PersonState { get; set; }

    public virtual Processor Process { get; set; } = null!;
}
