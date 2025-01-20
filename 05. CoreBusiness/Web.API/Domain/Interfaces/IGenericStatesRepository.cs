using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IGenericStatesRepository
    {
        Task<RequestResult> GetStatesHealthCareStaff();
        Task<RequestResult> GetStatesAttention();
        Task<RequestResult> GetStatesProcess();
    }
}
