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

        [HttpGet("GetAttentionsFinishByHealthCareStaff")]
        public async Task<RequestResult> GetStadisticsByHealthCareStaff(Guid? BusinessLine) => await _IMonitoringRepository.GetAttentionsFinishByHealthCareStaff(BusinessLine);

        [HttpGet("GetLogguedByHealthCareStaff")]
        public async Task<RequestResult> GetLogguedByHealthCareStaff(Guid? BusinessLine) => await _IMonitoringRepository.GetLogguedHealthCareStaff(BusinessLine);

        [HttpGet("GetAttentionsByTimeLine")]
        public async Task<RequestResult> GetAttentionsByTimeLine(Guid? BusinessLine) => await _IMonitoringRepository.GetAttentionsByTimeLine(BusinessLine);

        [HttpGet("GetPercentAttentionsFinish")]
        public async Task<RequestResult> GetPercentAttentionsFinish(Guid? BusinessLine) => await _IMonitoringRepository.GetPercentAttentionsFinish(BusinessLine);

        [HttpGet("GetNumberAttentionsByCity")]
        public async Task<RequestResult> GetNumberAttentionsByCity(Guid? BusinessLine) => await _IMonitoringRepository.GetNumberAttentionsByCity(BusinessLine);


        [HttpGet("GetQueuesActive")]
        public async Task<RequestResult> GetQueuesActive(Guid? BusinessLineId) => await _IMonitoringRepository.GetQueuesActive(BusinessLineId);

        [HttpGet("GetNumberActive")]
        public async Task<RequestResult> GetNumberActive(Guid? BusinessLineId) => await _IMonitoringRepository.GetNumberActive(BusinessLineId);


        
    }
}
