using Shared;
using Web.Core.Business.API.Enums;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IEmitMessagesRepository
    {
        Task<RequestResult> CreateAttention(string processCode, Guid patientId);
        Task<RequestResult> AssignAttention(Guid HealthCareStaffId);
        Task<RequestResult> StartAttention(Guid AttentionId);
        Task<RequestResult> FinishAttention(Guid AttentionId);
        Task<RequestResult> CancelAttention(Guid AttentionId);
    }
}
