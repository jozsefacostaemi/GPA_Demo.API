using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Monitoring
{
    public class MonitoringRepository : IMonitoringRepository
    {
        private readonly ApplicationDbContext _context;
        public MonitoringRepository(ApplicationDbContext context) => _context = context;

        /* Función que valida las credenciales del personal asistencial */
        public async Task<RequestResult> GetQuantityByState(Guid? BusinessLineId)
        {
            Guid BusinessLine = BusinessLineId.HasValue ? BusinessLineId.Value : Guid.Parse("DD44C571-4FA5-4133-AECD-062834C93601");
            Guid levelQueueByBusinessLine = await _context.BusinessLines.Where(x => x.Id.Equals(BusinessLine)).Select(x => x.LevelQueueId).FirstOrDefaultAsync();
            var getStatesAttention = await _context.AttentionStates.Where(x => x.Active == true).Select(x => new { x.Id, x.Name }).ToListAsync();
            if (!getStatesAttention.Any())
                return RequestResult.SuccessResultNoRecords();

            var attentionsByState = await _context.Attentions
                .Where(x => getStatesAttention.Select(s => s.Id).Contains(x.AttentionStateId))
                .GroupBy(x => new { x.AttentionStateId, x.AttentionState.Name, x.AttentionState.Color })
                .Select(g => new
                {
                    StateName = g.Key.Name,
                    Count = g.Count(),
                    Color = g.Key.Color
                })
                .ToListAsync();

            if (attentionsByState.Any())
                return RequestResult.SuccessResult(data: attentionsByState);
            return RequestResult.SuccessResultNoRecords();
        }
        /* Función que obtiene información de la CPU */
        public async Task<RequestResult> GetUsageCPU()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            float cpuUsage = cpuCounter.NextValue();
            return RequestResult.SuccessResult(cpuUsage);
        }
        /* Función que consulta la información de doctores agrupados por atenciones */
        public async Task<RequestResult> GetAttentionsFinishByHealthCareStaff(Guid? BusinessLineId)
        {
            var attentionsFinishByHealthCareStaff = await _context.Attentions
                .Where(x => x.HealthCareStaff != null && !string.IsNullOrEmpty(x.AttentionState.Code) && x.AttentionState.Code.Equals(AttentionStateEnum.FINA.ToString()))
                .GroupBy(x => new { x.HealthCareStaffId, x.HealthCareStaff.Name })
                .Select(x => new
                {
                    HealthCareStaffName = x.Key.Name,
                    Count = x.Count()
                }).ToListAsync();

            var formattedResult = attentionsFinishByHealthCareStaff.Select(x => new
            {
                HealthCareStaff = x.HealthCareStaffName,
                x.Count
            }).ToList();

            return RequestResult.SuccessResult(formattedResult);
        }
        /* Función que consulta la información de doctores logueados por atenciones */
        public async Task<RequestResult> GetLogguedHealthCareStaff(Guid? BusinessLineId)
        {
            var groupedUsers = await _context.HealthCareStaffs
                .GroupBy(u => u.Loggued)
                .Select(g => new
                {
                    Logged = g.Key,
                    Users = g.ToList()
                })
                .ToListAsync();
            var loggedUsers = groupedUsers.FirstOrDefault(g => g.Logged == true)?.Users;
            var notLoggedUsers = groupedUsers.FirstOrDefault(g => g.Logged == false)?.Users;
            return RequestResult.SuccessResult(data: new { loggedUsers, notLoggedUsers });
        }

        /* Función que consulta los datos de de números de atenciones en el tiempo */
        public async Task<RequestResult> GetAttentionsByTimeLine(Guid? BusinessLineId)
        {
            var result = await _context.Attentions
                .Where(a => a.CreatedAt >= DateTime.Now.AddDays(-10))
                .GroupBy(a => new { Year = a.CreatedAt.Value.Year, Month = a.CreatedAt.Value.Month, Day = a.CreatedAt.Value.Day })
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date.Year)
                .ThenBy(x => x.Date.Month)
                .ThenBy(x => x.Date.Day)
                .ToListAsync();
            return RequestResult.SuccessResult(result?.Select(x => new
            {
                YearMonth = $"{x.Date.Year}-{x.Date.Month:00}-{x.Date.Day:00}",
                x.Count
            }));
        }

        /* Función que consulta los datos de las atenciones finalizadas */
        public async Task<RequestResult> GetPercentAttentionsFinish(Guid? BusinessLineId)
        {
            var resultado = await _context.Attentions
                .GroupBy(a => a.AttentionState.Code)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            var totalAtenciones = resultado.Sum(r => r.Count);
            var atencionesFinalizadas = resultado.FirstOrDefault(r => r.Status.Equals(AttentionStateEnum.FINA.ToString()))?.Count ?? 0;
            double porcentajeAtencionesFinalizadas = 0;
            if (totalAtenciones > 0)
                porcentajeAtencionesFinalizadas = (double)atencionesFinalizadas / totalAtenciones * 100;
            return RequestResult.SuccessResult(porcentajeAtencionesFinalizadas);
        }

        /* Función que consulta los datos de las atenciones finalizadas */
        public async Task<RequestResult> GetNumberAttentionsByCity(Guid? BusinessLineId)
        {
            var resultByCity = await _context.Attentions
                .GroupBy(a => a.Patient.City.Name)
                .Select(g => new
                {
                    City = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            return RequestResult.SuccessResult(resultByCity?.Select(x => new
            {
                City = x.City,
                x.Count
            }));
        }

    }
}
