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
        [HttpGet("GetUsageCPU")]
        public async Task<RequestResult> GetCpuUsage() => await _IMonitoringRepository.GetUsageCPU();

        [HttpGet("GetQuantityByState")]
        public async Task<RequestResult> GetAttentions(Guid? BusinessLine) => await _IMonitoringRepository.GetQuantityByState(BusinessLine);
        
        [HttpGet("GetStadisticsByHealthCareStaff")]
        public async Task<RequestResult> GetStadisticsByHealthCareStaff(Guid? BusinessLine) => await _IMonitoringRepository.GetStadisticsByHealthCareStaff(BusinessLine);
        
        [HttpGet("GetLogguedByHealthCareStaff")]
        public async Task<RequestResult> GetLogguedByHealthCareStaff(Guid? BusinessLine) => await _IMonitoringRepository.GetLogguedHealthCareStaff(BusinessLine);

        [HttpGet("GetAttentionsByTimeLine")]
        public async Task<RequestResult> GetAttentionsByTimeLine(Guid? BusinessLine) => await _IMonitoringRepository.GetAttentionsByTimeLine(BusinessLine);

        

    }
}
