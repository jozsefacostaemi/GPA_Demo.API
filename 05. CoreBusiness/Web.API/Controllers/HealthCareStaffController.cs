using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;

namespace Web.Core.Business.API.Controllers
{
    [Route("HealCareStaffs")]
    public class HealthCareStaffController : Controller
    {
        private readonly IHealthCareStaffRepository _ihealthCareStaffRepository;
        public HealthCareStaffController(IHealthCareStaffRepository ihealthCareStaffRepository)
        {
            _ihealthCareStaffRepository = ihealthCareStaffRepository;
        }
        [HttpGet("GetStateByHealthCareStaff")]
        public async Task<RequestResult> GetStateByHealthCareStaff(Guid HealthCareStaff) => await _ihealthCareStaffRepository.GetStateByHealthCareStaff(HealthCareStaff);

        [HttpPost("UpdateStateForHealthCareStaff")]
        public async Task<RequestResult> UpdateStateForHealthCareStaff(Guid HealthCareStaff, string codeHealthCareStaff) => await _ihealthCareStaffRepository.UpdateStateForHealthCareStaff(HealthCareStaff, codeHealthCareStaff);
    }
}
