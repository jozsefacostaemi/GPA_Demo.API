using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Helpers;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core
{
    public class AttentionRepository : IAttentionRepository
    {
        private readonly ApplicationDbContext _context;
        public AttentionRepository(ApplicationDbContext context) => _context = context;

        /* Función que devuelve la información de las atenciones */
        public async Task<RequestResult> GetAttentions(string processCode, string excludeCodes)
        {
            var lstExcludeStates = string.IsNullOrEmpty(excludeCodes) ? null : excludeCodes.Split(',');
            var getAttentions = await _context.Attentions
                .Where(z => !string.IsNullOrEmpty(processCode) ? z.Process.Code.Equals(processCode) : true)
                .Where(z => lstExcludeStates != null ? !lstExcludeStates.Contains(z.AttentionState.Code) : true)
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.Priority)
                .Select(x => new AttentionResponse
                {
                    AttentionId = x.Id,
                    Priority = x.Priority,
                    HealthCareStaff = x.HealthCareStaff != null ? x.HealthCareStaff.Name : "N/A",
                    Patient = x.Patient != null ? x.Patient.Name : "N/A",
                    Process = x.Process != null ? x.Process.Name : string.Empty,
                    City = x.Patient != null && x.Patient.City != null ? x.Patient.City.Name : string.Empty,
                    Comorbities = x.Patient != null && x.Patient.Comorbidities != null ? (int)x.Patient.Comorbidities : 0,
                    Age = x.Patient != null && x.Patient.Birthday != null ? CalculatedAge.YearsMonthsDays(Convert.ToDateTime(x.Patient.Birthday)) : string.Empty,
                    State = x.AttentionState != null ? x.AttentionState.Name : string.Empty,
                    Plan = x.Patient != null && x.Patient.Plan != null ? x.Patient.Plan.Name : "N/A",
                    StartDate = x.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndDate = x.EndDate != null ? $"{x.EndDate.Value.ToString()}" : string.Empty
                })
                .ToListAsync();
            if (!getAttentions.Any())
                return RequestResult.SuccessResultNoRecords();
            return RequestResult.SuccessResult(data: getAttentions);

        }
    }
}
