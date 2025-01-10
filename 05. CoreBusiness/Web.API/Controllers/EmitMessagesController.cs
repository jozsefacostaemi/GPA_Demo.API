using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;

namespace Web.Queue.API.Controllers
{
    [Route("EmitMessages")]
    public class EmitMessagesController : ControllerBase
    {
        private readonly IEmitMessagesRepository _emitMessageRepository;
        public EmitMessagesController(IEmitMessagesRepository emitMessagesRepository)
        {
            _emitMessageRepository = emitMessagesRepository;
        }

        [HttpPost("EmitAttention")]
        public async Task<bool> EmitAttention(ProcessEnum processEnum, Guid PatientId)
        {
            return await _emitMessageRepository.EmitAttention(processEnum, PatientId);
        }

        [HttpPost("AssignAttention")]
        public async Task<bool> AssignAttention(Guid HealthCareStaffId)
        {
            return await _emitMessageRepository.AssignAttention(HealthCareStaffId);
        }

        [HttpPost("StartAttention")]
        public async Task<bool> StartAttention(Guid AttentionId)
        {
            return await _emitMessageRepository.StartAttention(AttentionId);
        }

        [HttpPost("FinishAttention")]
        public async Task<bool> FinishAttention(Guid AttentionId)
        {
            return await _emitMessageRepository.FinishAttention(AttentionId);
        }
    }
}
