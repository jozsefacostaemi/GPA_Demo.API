using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;

namespace Web.Core.Business.API.Controllers
{
    [Route("Monitoring")]
    public class MonitoringController : Controller
    {
        private readonly IMonitoringRepository _IMonitoringRepository;

        public MonitoringController(IMonitoringRepository IMonitoringRepository)
        {
            _IMonitoringRepository = IMonitoringRepository;
        }

        [HttpGet("GetQuantityByState")]
        public async Task<RequestResult> GetAttentions(string processCode, Guid? BusinessLine) =>
            await _IMonitoringRepository.GetQuantityByState(processCode, BusinessLine);
    }
}
