using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using Web.Core.Business.API.Domain.Interfaces;

namespace Web.Core.Business.API.Controllers
{
    [Route("Queue")]
    public class QueueController : ControllerBase
    {
        private readonly IQueueRepository _queueRepository;
        public QueueController(IQueueRepository queueRepository)
        {
            _queueRepository = queueRepository;
        }

        [HttpPost("GeneratedConfigQueues")]
        public async Task<bool> GeneratedConfigQueues() => await _queueRepository.GeneratedConfigQueues();

        [HttpPost("CreatedQueues")]
        public async Task<bool> CreatedQueues() => await _queueRepository.CreatedQueues();

        [HttpDelete("DeleteQueues")]
        public async Task<bool> DeleteQueues() => await _queueRepository.DeleteQueues();
    }
}
