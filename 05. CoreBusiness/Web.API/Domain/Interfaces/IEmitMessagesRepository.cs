using Shared;
using Web.Core.Business.API.Enums;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IEmitMessagesRepository
    {
        Task<RequestResult> CreateAttention(ProcessEnum processEnum, Guid patientId);
        Task<RequestResult> AssignAttention(Guid HealthCareStaffId);
        Task<RequestResult> InitAttention(Guid AttentionId);
        Task<RequestResult> EndAttention(Guid AttentionId);
        Task<RequestResult> CancelAttention(Guid AttentionId);
        Task<RequestResult> AvailableHealthCareScaff(Guid HealthCareStaffId);
    }
}
