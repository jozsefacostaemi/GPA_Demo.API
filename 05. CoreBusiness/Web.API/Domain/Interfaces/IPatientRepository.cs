using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IPatientRepository
    {
        Task<RequestResult> GetPatients(string? identification);
    }
}
