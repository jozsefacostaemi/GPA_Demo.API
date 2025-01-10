using Lib.MessageQueues.Functions.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Diagnostics;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Queue.API.Infraestructure.Repositories
{
    public class EmitMessageRepository : IEmitMessagesRepository
    {
        #region Variables
        private readonly ApplicationDbContext _context;
        private readonly IRabbitMQFunctions _rabbitMQFunctions;
        #endregion

        #region Ctor
        public EmitMessageRepository(ApplicationDbContext context, IRabbitMQFunctions rabbitMQFunctions)
        {
            _context = context;
            _rabbitMQFunctions = rabbitMQFunctions;
        }
        #endregion

        #region Public Methods
        /* Función que dispara mensaje en cola Pendiente según el proceso seleccionado */
        public async Task<bool> EmitAttention(ProcessEnum processEnum, Guid patientId)
        {
            var patient = await _context.Patients.Include(x => x.City).Select(x => new { x.Id, x.CityId, PlanCode = x.Plan.Code, x.Birthday, x.Comorbidities }).Where(x => x.Id == patientId).FirstOrDefaultAsync();
            var process = await _context.Processors.Where(x => x.Code == processEnum.ToString()).FirstOrDefaultAsync();
            var state = await _context.AttentionStates.Where(x => x.Code == AttentionStateEnum.PEND.ToString()).FirstOrDefaultAsync();
            var PersonState = await _context.PersonStates.Where(x => x.Code == PersonStateEnum.ASIG.ToString()).FirstOrDefaultAsync();

            if (patient == null || process == null || state == null || PersonState == null)
                return false;

            var getNameQueueGenerated = await GetGeneratedQueue(process.Id, (Guid)patient.CityId, state.Id);

            if (string.IsNullOrEmpty(getNameQueueGenerated))
                return false;
            var attentionId = await CreateAttention(patientId, state.Id, process.Name);
            await InsertHistoryAttention(attentionId, state.Id);
            var planRecord = patient.PlanCode == PlanEnum.BAS.ToString() ? 1 : patient.PlanCode == PlanEnum.STA.ToString() ? 2 : patient.PlanCode == PlanEnum.PRE.ToString() ? 3 : 0;
            await _rabbitMQFunctions.EmitMessagePending(getNameQueueGenerated, attentionId, patientId, (DateTime)patient.Birthday, patient.Comorbidities, planRecord, (Guid)patient.CityId, (Guid)process.Id);
            return true;
        }
        /* Función que dispara mensaje en cola Asignado según el proceso seleccionado */
        public async Task<bool> AssignAttention(Guid HealthCareStaffId)
        {
            var HealthCareStaff = await _context.HealthCareStaffs.Select(x => new { x.CityId, x.ProcessId, x.Id }).Where(x => x.Id.Equals(HealthCareStaffId)).FirstOrDefaultAsync();
            if (HealthCareStaff == null)
                return false;
            var StateAsig = await GetAttentionStateByCode(AttentionStateEnum.ASIG.ToString());
            if (StateAsig == null)
                return false;
            var StatePend = await GetAttentionStateByCode(AttentionStateEnum.PEND.ToString());
            if (StatePend == null)
                return false;
            //Consultamos nombre de cola Asignada
            var getNameQueueAsignedGenerated = await GetGeneratedQueue(HealthCareStaff.ProcessId, HealthCareStaff.CityId, StateAsig.Id);
            if (string.IsNullOrEmpty(getNameQueueAsignedGenerated))
                return false;
            //Consultamos nombre de cola En Proceso
            var getNameQueuePendingGenerated = await GetGeneratedQueue(HealthCareStaff.ProcessId, HealthCareStaff.CityId, StatePend.Id);
            if (string.IsNullOrEmpty(getNameQueuePendingGenerated))
                return false;
            //Se asigna cita a estado Asignado
            string resultAttention = await _rabbitMQFunctions.EmitMessageAsign(getNameQueueAsignedGenerated, getNameQueuePendingGenerated, HealthCareStaffId);
            if (string.IsNullOrEmpty(resultAttention)) return false;
            await AssignHealthCareStaffIdToAttention(Guid.Parse(resultAttention), HealthCareStaffId);
            await InsertHistoryAttention(Guid.Parse(resultAttention), StateAsig.Id);
            var inProcessAttentionPerson = await GetPersonStateByCode(PersonStateEnum.ENPRO.ToString());
            if (inProcessAttentionPerson == null) return false;
            await MachineState(Guid.Parse(resultAttention), StateAsig.Id, HealthCareStaffId, inProcessAttentionPerson.Id);
            Console.WriteLine(resultAttention);
            return true;
        }

        /* Función que dispara mensaje en cola En Proceso según el proceso seleccionado */
        public async Task<bool> StartAttention(Guid AttentionId)
        {
            var infoAttention = await _context.Attentions.Include(x => x.HealthCareStaff).Where(x => x.Id.Equals(AttentionId)).Select(x => new { x.HealthCareStaff.CityId, x.HealthCareStaff.ProcessId }).FirstOrDefaultAsync();
            if (infoAttention == null) return false;
            var StateInPro = await GetAttentionStateByCode(AttentionStateEnum.ENPRO.ToString());
            if (StateInPro == null) return false;
            var getNameQueueInProGenerated = await GetGeneratedQueue(infoAttention.ProcessId, infoAttention.CityId, StateInPro.Id);
            if (getNameQueueInProGenerated == null) return false;
            var StateAsig = await GetAttentionStateByCode(AttentionStateEnum.ASIG.ToString());
            if (StateAsig == null) return false;
            var getNameQueueAsigGenerated = await GetGeneratedQueue(infoAttention.ProcessId, infoAttention.CityId, StateAsig.Id);
            if (getNameQueueAsigGenerated == null) return false;
            await _rabbitMQFunctions.EmitMessageInProcess(AttentionId, getNameQueueAsigGenerated, getNameQueueInProGenerated);
            await InsertHistoryAttention(AttentionId, StateInPro.Id);
            Console.WriteLine(AttentionId);
            return true;
        }
        /* Función que dispara mensaje en cola Finalizado según el proceso seleccionado */
        public async Task<bool> FinishAttention(Guid AttentionId)
        {
            var infoAttention = await _context.Attentions.Where(x => x.Id.Equals(AttentionId)).Select(x => new { x.HealthCareStaff.CityId, x.HealthCareStaff.ProcessId, x.HealthCareStaffId }).FirstOrDefaultAsync();
            if (infoAttention == null) return false;
            var StateFinish = await GetAttentionStateByCode(AttentionStateEnum.FINA.ToString());
            if (StateFinish == null) return false;
            var getNameQueueFinishGenerated = await GetGeneratedQueue(infoAttention.ProcessId, infoAttention.CityId, StateFinish.Id);
            if (getNameQueueFinishGenerated == null) return false;
            var StateInPro = await GetAttentionStateByCode(AttentionStateEnum.ENPRO.ToString());
            if (StateInPro == null) return false;
            var getNameQueueInProGenerated = await GetGeneratedQueue(infoAttention.ProcessId, infoAttention.CityId, StateInPro.Id);
            if (getNameQueueInProGenerated == null) return false;
            var finishAttentionPerson = await GetPersonStateByCode(PersonStateEnum.DISP.ToString());
            if (finishAttentionPerson == null) return false;
            await _rabbitMQFunctions.EmitMessageFinish(AttentionId, getNameQueueInProGenerated, getNameQueueFinishGenerated);
            await MachineState(AttentionId, StateFinish.Id, (Guid)infoAttention.HealthCareStaffId, finishAttentionPerson.Id);
            await InsertHistoryAttention(AttentionId, StateFinish.Id);
            Console.WriteLine(AttentionId);
            return true;
        }
        #endregion

        #region Private Methods
        /* Función que obtiene el nombre de la cola con base al proceso, ciudad y estado */
        private async Task<string> GetGeneratedQueue(Guid processId, Guid cityId, Guid StateId)
        {
            return await _context.GeneratedQueues.Where(x => x.ConfigQueue.ProcessId.Equals(processId) && x.ConfigQueue.AttentionStateId.Equals(StateId) && x.ConfigQueue.CityId.Equals(cityId)).Select(x => x.Name).FirstOrDefaultAsync();
        }
        /* Función que consulta el estado de atención por código */
        private async Task<dynamic?> GetAttentionStateByCode(string state)
        {
            return await _context.AttentionStates
             .Select(x => new { x.Id, x.Code })
             .Where(x => x.Code.Equals(state))
             .FirstOrDefaultAsync();
        }

        /* Función que consulta el estado de la persona por código */
        private async Task<dynamic?> GetPersonStateByCode(string state)
        {
            return await _context.PersonStates
             .Select(x => new { x.Id, x.Code })
             .Where(x => x.Code.Equals(state))
             .FirstOrDefaultAsync();
        }
        /* Función que guarda la atención  */
        private async Task<Guid> CreateAttention(Guid PatientId, Guid State, string Origin)
        {
            var attention = new Attention
            {
                Id = Guid.NewGuid(),
                PatientId = PatientId,
                Open = true,
                StartDate = DateTime.Now,
                Comments = "Cita creada desde: " + Origin,
                Active = true,
                AttentionStateId = State
            };
            await _context.AddAsync(attention);
            await _context.SaveChangesAsync();
            return attention.Id;
        }
        /* Función que guarda el hisotorico de atenciones*/
        private async Task InsertHistoryAttention(Guid AttentionId, Guid AttentionStateId)
        {
            await _context.AddAsync(new AttentionHistory { Id = Guid.NewGuid(), AttentionId = AttentionId, CreatedAt = DateTime.Now, Active = true, AttentionState = AttentionStateId });
            await _context.SaveChangesAsync();
        }
        /* Función que asinga el personal asistencial a una atención (Transición de estado Pendiente a Asignado) */
        private async Task AssignHealthCareStaffIdToAttention(Guid AttentionId, Guid HealthCareStaffId)
        {
            var attention = await _context.Attentions.Where(x => x.Id.Equals(AttentionId)).FirstOrDefaultAsync();
            if (attention != null)
            {
                attention.HealthCareStaffId = HealthCareStaffId;
                await _context.SaveChangesAsync();
            }
        }
        /* Función que guarda la información de la maquina de estados */
        private async Task MachineState(Guid AttentionId, Guid AttentionStateId, Guid HealthCareStaffId, Guid finishAttentionPersonState)
        {
            var attention = await _context.Attentions.Where(x => x.Id.Equals(AttentionId)).FirstOrDefaultAsync();
            if (attention != null)
            {
                attention.AttentionStateId = AttentionStateId;
                attention.Open = false;
                await _context.SaveChangesAsync();
            }
            var HealthCareStaff = await _context.HealthCareStaffs.Where(x => x.Id.Equals(HealthCareStaffId)).FirstOrDefaultAsync();
            if (HealthCareStaff != null)
            {
                HealthCareStaff.PersonStateId = finishAttentionPersonState;
                await _context.SaveChangesAsync();
            }
        }
        #endregion
    }
}
