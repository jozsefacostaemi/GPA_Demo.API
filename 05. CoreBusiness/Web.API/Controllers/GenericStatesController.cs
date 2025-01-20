using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;

namespace Web.Core.Business.API.Controllers
{
    [Route("GenericStates")]
    public class GenericStatesController : ControllerBase
    {
        private readonly IGenericStatesRepository _genericStatesRepository;

        public GenericStatesController(IGenericStatesRepository genericStatesRepository)
        {
            _genericStatesRepository = genericStatesRepository;
        }

        [HttpGet("GetStatesHealthCareStaff")]
        public async Task<RequestResult> GetStatesHealthCareStaff() => await _genericStatesRepository.GetStatesHealthCareStaff();

        [HttpGet("GetStatesAttention")]
        public async Task<RequestResult> GetStatesAttention() => await _genericStatesRepository.GetStatesAttention();

        [HttpGet("GetStatesProcess")]
        public async Task<RequestResult> GetStatesProcess() => await _genericStatesRepository.GetStatesProcess();

    }
}
