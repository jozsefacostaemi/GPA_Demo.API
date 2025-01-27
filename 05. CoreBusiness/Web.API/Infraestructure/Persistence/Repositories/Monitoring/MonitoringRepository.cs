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
        public async Task<RequestResult> GetQuantityByState(string? processCode, Guid? BusinessLineId)
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
    }
}
