using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.DTOs.Input;

namespace Web.Core.Business.API.Controllers
{
    [Route("HealCareStaffs")]
    public class HealthCareStaffController : Controller
    {
        private readonly IHealthCareStaffRepository _ihealthCareStaffRepository;
        public HealthCareStaffController(IHealthCareStaffRepository ihealthCareStaffRepository) =>
            _ihealthCareStaffRepository = ihealthCareStaffRepository;

        [HttpGet("SearchFirstHealCareStaffAvailable")]
        public async Task<RequestResult> SearchFirstHealCareStaffAvailable() => await _ihealthCareStaffRepository.SearchFirstHealCareStaffAvailable();

        [HttpGet("GetStateByHealthCareStaff")]
        public async Task<RequestResult> GetStateByHealthCareStaff(Guid HealthCareStaff) => await _ihealthCareStaffRepository.GetStateByHealthCareStaff(HealthCareStaff);

        [HttpPost("UpdateStateForHealthCareStaff")]
        public async Task<RequestResult> UpdateStateForHealthCareStaff(RequestHealthCareStaffsUpdateDTO requestHealthCareStaffsUpdateDTO) => await _ihealthCareStaffRepository.UpdateStateForHealthCareStaff(requestHealthCareStaffsUpdateDTO.HealthCareStaff, requestHealthCareStaffsUpdateDTO.codeHealthCareStaff);

    }
}
