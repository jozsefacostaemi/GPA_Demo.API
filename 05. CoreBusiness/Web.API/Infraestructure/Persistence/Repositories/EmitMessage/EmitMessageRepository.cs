using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Models;
using Lib.MessageQueues.Functions.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared;
using System.ComponentModel.DataAnnotations;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.StateMachine;
using Web.Core.Business.API.Infraestructure.Persistence.Validators;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.EmitMessage
{
    public class EmitMessageRepository : IEmitMessagesRepository
    {
        #region Variables
        private readonly ApplicationDbContext _context;
        private readonly IMessagingFunctions _messagingFunctions;
        private readonly GetMachineStateValidator _getMachineStateValidator;
        private readonly GetStatesRepository _getStatesRepository;
        #endregion

        #region Ctor
        public EmitMessageRepository(ApplicationDbContext context, MessagingFunctionsFactory messagingFunctionsFactory, GetMachineStateValidator getMachineStateValidator, GetStatesRepository getStatesRepository)
        {
            _context = context;
            _messagingFunctions = messagingFunctionsFactory.GetMessagingFunctions();
            _getMachineStateValidator = getMachineStateValidator;
            _getStatesRepository = getStatesRepository;
        }
        #endregion

        #region Public Methods
        /* Función que dispara mensaje en cola Pendiente según el proceso seleccionado */
        public async Task<RequestResult> CreateAttention(ProcessEnum processEnum, Guid patientId)
        {
            var validate = await _getMachineStateValidator.CreationPreconditions(patientId);
            if (!validate.Item1)
                return RequestResult.ErrorResult(message: validate.Item2);
            var machineStates = await _getStatesRepository.GetMachineStates(StateEventProcessEnum.CREATION);
            if (machineStates == null) return RequestResult.ErrorResult($"No existe información para el proceso {StateEventProcessEnum.CREATION}");
            var patient = await GetInfoPatient(patientId);
            var process = await GetProcessor(processEnum.ToString());
            var getNameQueueGenerated = await GetGeneratedQueue(process.Id, (Guid)patient.CityId, (Guid)machineStates.attentionStateActualId);
            if (string.IsNullOrEmpty(getNameQueueGenerated))
                return RequestResult.ErrorResult(message: "No existe una cola para la ciudad, el proceso y el estado indicado");
            var attentionId = await CreateAttention(patientId, (Guid)machineStates.attentionStateActualId, process.Name);
            await InsertHistoryAttention(attentionId, (Guid)machineStates.attentionStateActualId);
            var planRecord = patient.PlanCode == PlanEnum.BAS.ToString() ? 1 : patient.PlanCode == PlanEnum.STA.ToString() ? 2 : patient.PlanCode == PlanEnum.PRE.ToString() ? 3 : 0;
            await _messagingFunctions.EmitMessagePending(getNameQueueGenerated, attentionId, patientId, (DateTime)patient.Birthday, patient.Comorbidities, planRecord, (Guid)patient.CityId, process.Id);
            await UpdateStates(attentionId, (Guid)machineStates.attentionStateActualId, null, null, (Guid)machineStates.patientStateId);
            return RequestResult.SuccessRecord(data: attentionId);
        }
        /* Función que dispara mensaje en cola Asignado según el proceso seleccionado */
        public async Task<RequestResult> AssignAttention(Guid HealthCareStaffId)
        {
            var validateChangeStatus = await _getMachineStateValidator.AsignationPreconditions(HealthCareStaffId);
            if (!validateChangeStatus.Item1)
                return RequestResult.ErrorResult(message: validateChangeStatus.Item2);
            StatesMachineResponse? machineStates = await _getStatesRepository.GetMachineStates(StateEventProcessEnum.ASIGNATION);
            if (machineStates == null) return RequestResult.ErrorResult($"No existe información para el proceso {StateEventProcessEnum.ASIGNATION}");
            var HealthCareStaff = await GetHealthCareStaffById(HealthCareStaffId);
            if (HealthCareStaff == null) return RequestResult.ErrorResult($"No existe información para el personal asistencial: {HealthCareStaffId}");
            var (getNameQueueAsignedGenerated, getNameQueuePendingGenerated) = await GetGeneratedQueues((Guid)HealthCareStaff.ProcessId, (Guid)HealthCareStaff.CityId, (Guid)machineStates.attentionStateActualId, (Guid)machineStates.attentionStatePreviousId);
            if (string.IsNullOrEmpty(getNameQueueAsignedGenerated) || string.IsNullOrEmpty(getNameQueuePendingGenerated)) return RequestResult.ErrorResult($"No existen colas configuradas para el proceso, ciudad e información para el evento de proceso {StateEventProcessEnum.ASIGNATION}");
            string resultEmitMessageAttention = await _messagingFunctions.EmitMessageAsign(getNameQueueAsignedGenerated, getNameQueuePendingGenerated, HealthCareStaffId);
            if (string.IsNullOrEmpty(resultEmitMessageAttention)) return RequestResult.ErrorResult($"No se encontró información para la cola de asignación");
            await InsertHistoryAttention(Guid.Parse(resultEmitMessageAttention), (Guid)machineStates.attentionStateActualId);
            await UpdateStates(Guid.Parse(resultEmitMessageAttention), (Guid)machineStates.attentionStateActualId, HealthCareStaffId, (Guid)machineStates.healthCareStaffStateId, (Guid)machineStates.patientStateId);
            return RequestResult.SuccessRecord($"Asignación de cita exitosa:  {resultEmitMessageAttention}");
        }
        /* Función que dispara mensaje en cola En Proceso según el proceso seleccionado */
        public async Task<RequestResult> InitAttention(Guid AttentionId) => await EmitAttention(AttentionId, StateEventProcessEnum.INITIATION);
        /* Función que dispara mensaje en cola Finalizado según el proceso seleccionado */
        public async Task<RequestResult> EndAttention(Guid AttentionId) => await EmitAttention(AttentionId, StateEventProcessEnum.ENDING);
        /* Función que cancela la atención */
        public async Task<RequestResult> CancelAttention(Guid AttentionId) => await EmitAttention(AttentionId, StateEventProcessEnum.CANCELLATION);
        /* Función que actualiza el estado del personal asistencial a disponible */
        public async Task<RequestResult> AvailableHealthCareScaff(Guid HealthCareStaffId)
        {
            Guid? StateAvailable = await _context.PersonStates.Where(x => x.Code.Equals(PersonStateEnum.DISP.ToString())).Select(x => x.Id).SingleOrDefaultAsync();
            if (StateAvailable == null) return RequestResult.ErrorResult("No existe el estado Disponible");

            var HealthCareStaff = await _context.HealthCareStaffs.Include(x => x.PersonState).SingleOrDefaultAsync(x => x.Id.Equals(HealthCareStaffId));
            if (HealthCareStaff != null)
            {
                if (HealthCareStaff.PersonState.Code.Equals(PersonStateEnum.REC.ToString()))
                {
                    HealthCareStaff.PersonStateId = StateAvailable;
                    await _context.SaveChangesAsync();
                    return RequestResult.SuccessOperation();
                }
                else return RequestResult.ErrorResult("El estado del personal asistencial debe ser En receso");
            }
            return RequestResult.ErrorResult("No existe el personal asistencial indicado");
        }
        #endregion

        #region Private Methods

        /* Función que consulta el nombre de la cola actual y la cola previa */
        private async Task<(string? getNameQueueAsignedGenerated, string? getNameQueuePendingGenerated)> GetGeneratedQueues(Guid processId, Guid cityId, Guid currentStateId, Guid previousStateId)
        {
            var getNameQueueAsignedGenerated = await _context.GeneratedQueues
                .Where(x => x.ConfigQueue.ProcessId.Equals(processId) && x.ConfigQueue.AttentionStateId.Equals(currentStateId) && x.ConfigQueue.CityId.Equals(cityId))
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
            var getNameQueuePendingGenerated = await _context.GeneratedQueues
                            .Where(x => x.ConfigQueue.ProcessId.Equals(processId) && x.ConfigQueue.AttentionStateId.Equals(previousStateId) && x.ConfigQueue.CityId.Equals(cityId))
                            .Select(x => x.Name)
                            .FirstOrDefaultAsync();
            return (getNameQueueAsignedGenerated, getNameQueuePendingGenerated);
        }
        /* Función que obtiene el nombre de la cola con base al proceso, ciudad y estado */
        private async Task<string?> GetGeneratedQueue(Guid processId, Guid cityId, Guid StateId) => await _context.GeneratedQueues.Where(x => x.ConfigQueue.ProcessId.Equals(processId) && x.ConfigQueue.AttentionStateId.Equals(StateId) && x.ConfigQueue.CityId.Equals(cityId)).Select(x => x.Name).FirstOrDefaultAsync();
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
        /* Función que guarda el historico de atenciones*/
        private async Task InsertHistoryAttention(Guid AttentionId, Guid AttentionStateId)
        {
            await _context.AddAsync(new AttentionHistory { Id = Guid.NewGuid(), AttentionId = AttentionId, CreatedAt = DateTime.Now, Active = true, AttentionState = AttentionStateId });
            await _context.SaveChangesAsync();
        }
        /* Función que guarda la información de la maquina de estados */
        private async Task UpdateStates(Guid AttentionId, Guid AttentionStateId, Guid? HealthCareStaffId, Guid? newStateHealthCareStaff, Guid newStatePatient, bool applyClosed = false)
        {
            var attention = await _context.Attentions.FindAsync(AttentionId);
            if (attention != null && HealthCareStaffId.HasValue)
            {
                attention.AttentionStateId = AttentionStateId;
                attention.HealthCareStaffId = HealthCareStaffId;
                attention.Open = applyClosed ? false : attention.Open;
            }
            if (HealthCareStaffId.HasValue)
            {
                var HealthCareStaff = await _context.HealthCareStaffs.FindAsync(HealthCareStaffId);
                if (HealthCareStaff != null)
                    HealthCareStaff.PersonStateId = newStateHealthCareStaff;
            }
            var patient = await _context.Patients.FindAsync(attention.PatientId);
            if (patient != null)
                patient.PersonStateId = newStatePatient;
            await _context.SaveChangesAsync();
        }
        /* Función que consulta el proceso por código */
        private async Task<Processor?> GetProcessor(string code) => await _context.Processors.AsNoTracking().Where(x => x.Code == code).SingleOrDefaultAsync();
        private async Task<dynamic?> GetInfoPatient(Guid patientId) => await _context.Patients.AsNoTracking().Include(x => x.City).Select(x => new { x.Id, x.CityId, PlanCode = x.Plan.Code, x.Birthday, x.Comorbidities }).SingleOrDefaultAsync(x => x.Id == patientId);
        /* Función que consulta el personal asistencial por código */
        private async Task<dynamic?> GetHealthCareStaffById(Guid HealthCareStaffId) => await _context.HealthCareStaffs.AsNoTracking().Select(x => new { x.CityId, x.ProcessId, x.Id }).Where(x => x.Id.Equals(HealthCareStaffId)).SingleOrDefaultAsync();
        /* Función que consulta la atención por Id */
        private async Task<dynamic?> GetAttentionById(Guid AttentionId) => await _context.Attentions.AsNoTracking().Include(x => x.HealthCareStaff).Where(x => x.Id.Equals(AttentionId))
            .Select(x =>
            new
            {
                HealthCareStaffId = x.HealthCareStaffId != null ? x.HealthCareStaffId : Guid.Empty,
                CityId = x.HealthCareStaff != null ? x.HealthCareStaff.CityId : Guid.Empty,
                ProcessId = x.HealthCareStaff != null ? x.HealthCareStaff.ProcessId : Guid.Empty,
                PatientId = x.PatientId != null ? x.PatientId : Guid.Empty,
                AttentionStateId = x.AttentionStateId

            }).SingleOrDefaultAsync();
        /* Función que realiza proceso de proceso genericos */
        private async Task<RequestResult> EmitAttention(Guid AttentionId, StateEventProcessEnum eventProcess)
        {
            var infoAttention = await GetAttentionById(AttentionId);
            if (infoAttention == null) return RequestResult.ErrorResult("La atención indicada no existe");

            (bool, string) validate = eventProcess == StateEventProcessEnum.CANCELLATION ? await _getMachineStateValidator.CancelationPreconditions(infoAttention.PatientId, infoAttention.HealthCareStaffId, AttentionId) : eventProcess == StateEventProcessEnum.ENDING ? await _getMachineStateValidator.EndingPreconditions(infoAttention.PatientId, infoAttention.HealthCareStaffId, AttentionId) : eventProcess == StateEventProcessEnum.INITIATION ? await _getMachineStateValidator.InitiationPreconditions(infoAttention.PatientId, infoAttention.HealthCareStaffId, AttentionId) : (false, "El evento indicado no tiene precondiciones configuradas");
            if (!validate.Item1)
                return RequestResult.ErrorResult(message: validate.Item2);
            StatesMachineResponse? machineStates = await _getStatesRepository.GetMachineStates(eventProcess);
            if (machineStates == null) return RequestResult.ErrorResult($"No existe información para el evento de proceso {StateEventProcessEnum.CANCELLATION}");
            var (getNameQueueAsignedGenerated, getNameQueuePendingGenerated) = await GetGeneratedQueues((Guid)infoAttention.ProcessId, (Guid)infoAttention.CityId, (Guid)machineStates.attentionStateActualId, eventProcess == StateEventProcessEnum.CANCELLATION ? (Guid)infoAttention.AttentionStateId : (Guid)machineStates.attentionStatePreviousId);
            if (string.IsNullOrEmpty(getNameQueueAsignedGenerated) || string.IsNullOrEmpty(getNameQueuePendingGenerated)) return RequestResult.ErrorResult($"No existen colas configuradas para el proceso, ciudad e información para el evento de proceso {eventProcess}");
            await _messagingFunctions.EmitGenericMessage(AttentionId, getNameQueuePendingGenerated, getNameQueueAsignedGenerated);
            await InsertHistoryAttention(AttentionId, (Guid)machineStates.attentionStateActualId);
            await UpdateStates(AttentionId, (Guid)machineStates.attentionStateActualId, infoAttention.HealthCareStaffId, machineStates.healthCareStaffStateId, (Guid)machineStates.patientStateId, (eventProcess == StateEventProcessEnum.CANCELLATION || eventProcess == StateEventProcessEnum.ENDING) ? true : false);
            string result = eventProcess == StateEventProcessEnum.CANCELLATION ? "Cancelación de atención exitosa" : eventProcess == StateEventProcessEnum.ENDING ? "Finalización de atención exitosa" : eventProcess == StateEventProcessEnum.INITIATION ? "Inicio de atención exitosa" : "Proceso realizado con éxito";
            return RequestResult.SuccessRecord(result);
        }
        #endregion
    }
}
