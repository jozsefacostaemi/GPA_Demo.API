using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
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
        public async Task<RequestResult> EmitAttention(string processCode, Guid PatientId)
        {
            return await _emitMessageRepository.CreateAttention(processCode, PatientId);
        }

        [HttpPost("AssignAttention")]
        public async Task<RequestResult> AssignAttention(Guid HealthCareStaffId)
        {
            return await _emitMessageRepository.AssignAttention(HealthCareStaffId);
        }

        [HttpPost("StartAttention")]
        public async Task<RequestResult> StartAttention(Guid AttentionId)
        {
            return await _emitMessageRepository.InitAttention(AttentionId);
        }

        [HttpPost("FinishAttention")]
        public async Task<RequestResult> FinishAttention(Guid AttentionId)
        {
            return await _emitMessageRepository.EndAttention(AttentionId);
        }
        [HttpPost("CancelAttention")]
        public async Task<RequestResult> CancelAttention(Guid AttentionId)
        {
            return await _emitMessageRepository.CancelAttention(AttentionId);
        }
    }
}
