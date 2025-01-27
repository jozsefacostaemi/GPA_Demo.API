using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IMonitoringRepository
    {
        Task<RequestResult> GetQuantityByState(string? processCode, Guid? BusinessLineId);
    }
}
