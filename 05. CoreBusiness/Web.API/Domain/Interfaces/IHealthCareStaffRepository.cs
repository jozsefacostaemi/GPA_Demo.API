using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IHealthCareStaffRepository
    {
        Task<RequestResult> UpdateStateForHealthCareStaff(Guid HealthCareStaff, string codeHealthCareStaff);
    }
}
