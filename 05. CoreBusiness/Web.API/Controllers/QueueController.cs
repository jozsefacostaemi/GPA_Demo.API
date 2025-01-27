using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.DTOs.Input;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

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
    }
}
