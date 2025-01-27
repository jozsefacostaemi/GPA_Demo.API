using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.DTOs.Input;
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
        public async Task<RequestResult> EmitAttention([FromBody] RequestEmitMessagesDTO request)
        {
            return await _emitMessageRepository.CreateAttention(request.ProcessCode, request.PatientId);
        }

        [HttpPost("AssignAttention")]
        public async Task<RequestResult> AssignAttention([FromBody] RequestHealthCareStaffDTO request)
        {
            return await _emitMessageRepository.AssignAttention(request.HealthCareStaffId);
        }

        [HttpPost("StartAttention")]
        public async Task<RequestResult> StartAttention([FromBody] RequestAttentionDTO request)
        {
            return await _emitMessageRepository.InitAttention(request.AttentionId);
        }

        [HttpPost("FinishAttention")]
        public async Task<RequestResult> FinishAttention([FromBody] RequestAttentionDTO request)
        {
            return await _emitMessageRepository.EndAttention(request.AttentionId);
        }
        [HttpPost("CancelAttention")]
        public async Task<RequestResult> CancelAttention([FromBody] RequestAttentionDTO request)
        {
            return await _emitMessageRepository.CancelAttention(request.AttentionId);
        }
    }
}
