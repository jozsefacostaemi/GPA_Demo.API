using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;

namespace Web.Core.Business.API.Controllers
{
    [Route("Patients")]
    public class PatientController : Controller
    {
        private readonly IPatientRepository _ipatientRepository;
        public PatientController(IPatientRepository patientRepository)
        {
            _ipatientRepository = patientRepository;
        }

        [HttpGet("GetPatients")]
        public async Task<RequestResult> GetPatients(string? identification) => await _ipatientRepository.GetPatients(identification);

    }
}
