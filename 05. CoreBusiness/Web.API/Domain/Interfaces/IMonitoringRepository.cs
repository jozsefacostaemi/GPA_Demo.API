using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IMonitoringRepository
    {
        Task<RequestResult> GetUsageCPU();
        Task<RequestResult> GetQuantityByState(Guid? BusinessLineId);
        Task<RequestResult> GetStadisticsByHealthCareStaff(Guid? BusinessLineId);
        Task<RequestResult> GetLogguedHealthCareStaff(Guid? BusinessLineId);
        Task<RequestResult> GetAttentionsByTimeLine(Guid? BusinessLineId);
        


    }
}
