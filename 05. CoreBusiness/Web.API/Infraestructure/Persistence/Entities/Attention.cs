using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class Attention
{
    public Guid Id { get; set; }

    public Guid AttentionStateId { get; set; }

    public Guid PatientId { get; set; }

    public Guid? HealthCareStaffId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Comments { get; set; }

    public bool Open { get; set; }

    public bool? Active { get; set; }

    public Guid? ProcessId { get; set; }

    public int Priority { get; set; }

    public virtual ICollection<AttentionHistory> AttentionHistories { get; set; } = new List<AttentionHistory>();

    public virtual AttentionState AttentionState { get; set; } = null!;

    public virtual HealthCareStaff? HealthCareStaff { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Processor? Process { get; set; }
}
