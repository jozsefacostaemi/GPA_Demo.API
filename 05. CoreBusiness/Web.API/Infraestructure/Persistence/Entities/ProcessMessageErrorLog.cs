using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class ProcessMessageErrorLog
{
    public Guid Id { get; set; }

    public Guid ProcessMessageId { get; set; }

    public string ErrorMessage { get; set; } = null!;

    public string StackTrace { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ProcessMessage ProcessMessage { get; set; } = null!;
}
