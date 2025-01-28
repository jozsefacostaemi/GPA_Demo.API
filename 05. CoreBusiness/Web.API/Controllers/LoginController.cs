using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.DTOs.Input;

namespace Web.Core.Business.API.Controllers
{
    [Route("auth")]
    public class LoginController : Controller
    {
        private readonly ILoginRepository _ILoginRepository;
        public LoginController(ILoginRepository ILoginRepository)
        {
            _ILoginRepository = ILoginRepository;
        }
       
        [HttpPost("LogIn")]
        public async Task<RequestResult> LogIn([FromBody] RequestLoginDTO loginDto) => await _ILoginRepository.LogIn(loginDto.UserName, loginDto.Password);

        [HttpPost("LogOut")]
        public async Task<RequestResult> LogOut([FromBody]  RequestHealthCareStaffDTO logout) => await _ILoginRepository.LogOut(logout.HealthCareStaffId);


    }
}
