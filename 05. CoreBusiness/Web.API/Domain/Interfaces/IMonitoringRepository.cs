using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IMonitoringRepository
    {
        Task<RequestResult> GetUsageCPU();
        Task<RequestResult> GetQuantityByState(Guid? BusinessLineId);
        Task<RequestResult> GetAttentionsFinishByHealthCareStaff(Guid? BusinessLineId);
        Task<RequestResult> GetLogguedHealthCareStaff(Guid? BusinessLineId);
        Task<RequestResult> GetAttentionsByTimeLine(Guid? BusinessLineId);
        Task<RequestResult> GetPercentAttentionsFinish(Guid? BusinessLineId);
        Task<RequestResult> GetNumberAttentionsByCity(Guid? BusinessLineId);
        




    }
}
