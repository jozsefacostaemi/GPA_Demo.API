namespace Web.Core.Business.API.Domain.Interfaces
{
    public interface IQueueRepository
    {
        Task<bool> GeneratedConfigQueues();
        Task<bool> CreatedQueues();
        Task<bool> DeleteQueues();
    }
}
