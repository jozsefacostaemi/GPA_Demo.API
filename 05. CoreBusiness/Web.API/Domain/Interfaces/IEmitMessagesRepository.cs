using Web.Core.Business.API.Enums;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IEmitMessagesRepository
    {
        Task<bool> EmitAttention(ProcessEnum processEnum, Guid patientId);
        Task<bool> AssignAttention(Guid HealthCareStaffId);
        Task<bool> StartAttention(Guid AttentionId);
        Task<bool> FinishAttention(Guid AttentionId);
    }
}
