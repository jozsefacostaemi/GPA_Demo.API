using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface ILoginRepository
    {
        Task<RequestResult> LogIn(string userName, string password);
        Task<RequestResult> LogOut(Guid HealthCareStaffId);
    }
}
