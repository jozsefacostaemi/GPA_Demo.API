using System;
using System.Collections.Generic;

namespace Web.Core.Business.API.Infraestructure.Persistence.Entities;

public partial class Country
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
