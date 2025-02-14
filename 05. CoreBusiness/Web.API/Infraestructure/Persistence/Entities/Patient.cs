﻿using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class Patient
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid CityId { get; set; }

    public Guid? PlanId { get; set; }

    public DateTime? Birthday { get; set; }

    public int? Comorbidities { get; set; }

    public bool? Active { get; set; }

    public Guid? PersonStateId { get; set; }

    public string Identification { get; set; } = null!;

    public Guid BusinessLineId { get; set; }

    public virtual ICollection<Attention> Attentions { get; set; } = new List<Attention>();

    public virtual BusinessLine BusinessLine { get; set; } = null!;

    public virtual City City { get; set; } = null!;

    public virtual PersonState? PersonState { get; set; }

    public virtual Plan? Plan { get; set; }
}
