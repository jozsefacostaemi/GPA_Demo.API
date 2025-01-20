using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;

namespace Web.Core.Business.API.Controllers
{
    [Route("Attentions")]
    public class AttentionController : Controller
    {
        private readonly IAttentionRepository _attentionRepository;

        public AttentionController(IAttentionRepository attentionRepository)
        {
            _attentionRepository = attentionRepository;
        }

        [HttpGet("GetAttentions")]
        public async Task<RequestResult> GetAttentions(string processCode, string LstExcludeStates) => 
            await _attentionRepository.GetAttentions(processCode, LstExcludeStates);
    }
}
