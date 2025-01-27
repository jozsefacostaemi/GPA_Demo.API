using Shared;

namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IAttentionRepository
    {
        Task<RequestResult> GetAttentions(string processCode, string LstExcludeStates);
        Task ResetAttentionsAndPersonStatus();
    }
}
