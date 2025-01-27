using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Helpers;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core
{
    public class AttentionRepository : IAttentionRepository
    {
        private readonly ApplicationDbContext _context;
        public AttentionRepository(ApplicationDbContext context) => _context = context;

        #region Public Methods
        /* Función que devuelve la información de las atenciones */
        public async Task ResetAttentionsAndPersonStatus()
        {
            await deleteAttentionHistory();
            await deleteAttentions();
            await updateStatusPersons();

        }
        /* Función que elimina las atenciones y actualiza los estados de los pacientes y personales asistenciales a pendiente  */
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
                    StartDate = x.CreatedAt.HasValue ? x.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                    EndDate = x.EndDate != null ? $"{x.EndDate.Value.ToString()}" : string.Empty
                })
                .ToListAsync();
            if (!getAttentions.Any())
                return RequestResult.SuccessResultNoRecords();
            return RequestResult.SuccessResult(data: getAttentions);

        }
        #endregion

        #region Private Methods
        /* Función que elimina el historial de atenciones */
        private async Task deleteAttentionHistory()
        {
            var attentionHistories = await _context.AttentionHistories.ToListAsync();
            _context.AttentionHistories.RemoveRange(attentionHistories);
            await _context.SaveChangesAsync();
        }
        /* Función que elimina las atenciones */
        private async Task deleteAttentions()
        {
            var attentions = await _context.Attentions.ToListAsync();
            _context.Attentions.RemoveRange(attentions);
            await _context.SaveChangesAsync();
        }
        /* Función que actualiza el estado de medicos y pacientes */
        private async Task updateStatusPersons()
        {
            Guid? getStatusAvailable = await _context.PersonStates.Where(x => x.Code.Equals(PersonStateEnum.DISP.ToString())).Select(x => x.Id).FirstOrDefaultAsync();
            if (getStatusAvailable != null && getStatusAvailable != Guid.Empty)
            {
                await _context.HealthCareStaffs.ExecuteUpdateAsync(x => x.SetProperty(p => p.PersonStateId, getStatusAvailable).SetProperty(x=>x.Loggued, false).SetProperty(p => p.AvailableAt, null as DateTime?));
                await _context.Patients.ExecuteUpdateAsync(x => x.SetProperty(p => p.PersonStateId, getStatusAvailable));
            }
        }

        #endregion
    }
}
