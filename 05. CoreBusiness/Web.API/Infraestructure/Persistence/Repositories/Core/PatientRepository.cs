using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Helpers;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core
{
    public class PatientRepository : IPatientRepository
    {
        private readonly ApplicationDbContext _context;

        public PatientRepository(ApplicationDbContext context) => _context = context;

        /* Función que consulta un listado de pacientes o la información de un paciente en particular */
        public async Task<RequestResult> GetPatients(string? identification)
        {
            var result = await _context.Patients
                .Where(x => !string.IsNullOrEmpty(identification) ? x.Identification.Equals(identification) : true)
                .Select(x => new PatientResponse { Identification = x.Identification, Name = x.Name, Id = x.Id, Age = x.Birthday != null ? CalculatedAge.YearsMonthsDays(Convert.ToDateTime(x.Birthday)): string.Empty }).ToListAsync();
            if (!result.Any())
                return RequestResult.SuccessResultNoRecords();
            return RequestResult.SuccessResult(result);

        }
    }
}
