using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Notification.Lib;
using Web.Core.Business.API.Helpers;

namespace Web.Core.Business.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SignalRTokenController : ControllerBase
    {
        private readonly IHubContext<EventHub> _hubContext;
        private readonly IConfiguration _configuration;

        public SignalRTokenController(IHubContext<EventHub> hubContext, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _configuration = configuration;
        }

        [HttpGet("token")]
        public IActionResult GetToken()
        {
            var connectionString = _configuration["AzureSignalRConnectionString"];
            var token = SignalRTokenGenerator.GenerateToken(connectionString, "EventHub");

            return Ok(new { token });
        }
    }
}
