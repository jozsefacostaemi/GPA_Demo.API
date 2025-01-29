using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
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
        public async Task<RequestResult> GetStadisticsByHealthCareStaff(Guid? BusinessLineId)
        {
            //var getHeadCareStaff = await _context.he
            return RequestResult.SuccessResult();
        }
        /* Función que consulta la información de doctores logueados por atenciones */
        public async Task<RequestResult> GetLogguedHealthCareStaff(Guid? BusinessLineId)
        {
            return RequestResult.SuccessResult();
        }

        /* Función que consulta los datos de de números de atenciones en el tiempo */
        public async Task<RequestResult> GetAttentionsByTimeLine(Guid? BusinessLineId)
        {
            var result = await _context.Attentions
                .Where(a => a.CreatedAt >= DateTime.Now.AddDays(-10)) // Filtra los últimos 10 días
                .GroupBy(a => new { Year = a.CreatedAt.Value.Year, Month = a.CreatedAt.Value.Month, Day = a.CreatedAt.Value.Day }) // Agrupa por año, mes y día
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date.Year)
                .ThenBy(x => x.Date.Month)
                .ThenBy(x => x.Date.Day) 
                .ToListAsync();
                       var formattedResult = result.Select(x => new
                       {
                           YearMonth = $"{x.Date.Year}-{x.Date.Month:00}-{x.Date.Day:00}",
                           x.Count
                       }).ToList();


            return RequestResult.SuccessResult(formattedResult);
        }
    }
}
