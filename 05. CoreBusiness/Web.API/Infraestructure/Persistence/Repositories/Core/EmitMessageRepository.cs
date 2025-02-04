using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Models;
using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Helpers;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Notifications;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.StateMachine;
using Web.Core.Business.API.Infraestructure.Persistence.Validators;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core
{
    public class EmitMessageRepository : IEmitMessagesRepository
    {
        #region Variables
        private readonly NotificationRepository _NotificationRepository;
        private readonly ApplicationDbContext _context;
        private readonly IMessagingFunctions _messagingFunctions;
        private readonly GetMachineStateValidator _getMachineStateValidator;
        private readonly GetStatesRepository _getStatesRepository;
        private readonly IHealthCareStaffRepository _IHealthCareStaffRepository;
        #endregion

        #region Ctor
        public EmitMessageRepository(ApplicationDbContext context, MessagingFunctionsFactory messagingFunctionsFactory, GetMachineStateValidator getMachineStateValidator, GetStatesRepository getStatesRepository, NotificationRepository NotificationRepository, IHealthCareStaffRepository iHealthCareStaffRepository)
        {
            _context = context;
            _messagingFunctions = messagingFunctionsFactory.GetMessagingFunctions();
            _getMachineStateValidator = getMachineStateValidator;
            _getStatesRepository = getStatesRepository;
            _NotificationRepository = NotificationRepository;
            _IHealthCareStaffRepository = iHealthCareStaffRepository;
        }
        #endregion

        #region Public Methods
        /* Función que dispara mensaje en cola Pendiente según el proceso seleccionado */
        public async Task<RequestResult> CreateAttention(string processCode, Guid patientId)
        {
            var validate = await _getMachineStateValidator.CreationPreconditions(patientId);
            if (!validate.Item1)
                return RequestResult.ErrorResult(message: validate.Item2);
            var machineStates = await _getStatesRepository.GetMachineStates(StateEventProcessEnum.CREATION);
            if (machineStates == null) return RequestResult.ErrorResult($"No existe información para el proceso {StateEventProcessEnum.CREATION}");
            var patient = await GetInfoPatient(patientId);
            if (patient == null) return RequestResult.ErrorResult(message: "El ID del paciente indicado no existe");
            var process = await GetProcessor(processCode);
            if (process == null) return RequestResult.ErrorResult(message: "El código de proceso indicado no existe");
            (string, Guid?, Guid?) getNameQueueGenerated = await GetQueueNameConfig(processCode, patient, machineStates.attentionStateTargetId);
            if (string.IsNullOrEmpty(getNameQueueGenerated.Item1))
                return RequestResult.ErrorResult(message: "No existe una cola para la localidad, el proceso y el estado del paciente");
            var planRecord = patient.PlanCode == PlanEnum.BAS.ToString() ? 1 : patient.PlanCode == PlanEnum.STA.ToString() ? 2 : patient.PlanCode == PlanEnum.PRE.ToString() ? 3 : 0;
            int priority = calculatedPriority((DateTime)patient.Birthday, patient.Comorbidities, planRecord);
            var attentionId = await CreateAttention(process.Id, patientId, (Guid)machineStates.attentionStateTargetId, process.Name, priority, (Guid)getNameQueueGenerated.Item3);
            await InsertHistoryAttention(attentionId, (Guid)machineStates.attentionStateTargetId, (Guid)getNameQueueGenerated.Item2);
            await _messagingFunctions.EmitMessagePending(getNameQueueGenerated.Item1, attentionId, patientId, (Guid)patient.CityId, process.Id, (byte)priority);
            await UpdateMachineStates(attentionId, (Guid)machineStates.attentionStateTargetId, null, null, (Guid)machineStates.patientStateId);
            var resultAttention = await GetAttentionsById(attentionId);
           // await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);
            /* Si hay médico disponible, asignamos la cita automaticamente */
            var getHealCareStaffAvailable = await _IHealthCareStaffRepository.SearchFirstHealCareStaffAvailable();
            if (getHealCareStaffAvailable?.Data != null) return await AssignAttention((Guid)getHealCareStaffAvailable.Data);
            //else
                //await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);
            await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.Monitoring);
            return RequestResult.SuccessRecord(message: "Creación de atención exitosa", data: resultAttention);
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
            (string, Guid?, Guid?) getNameQueuePendingGenerated = await GetQueueNameConfig(HealthCareStaff.processCode, new { HealthCareStaff.LevelQueueCode, HealthCareStaff.DepartmentId, HealthCareStaff.CountryId, HealthCareStaff.CityId }, (Guid)machineStates.attentionStateActualId);
            (string, Guid?, Guid?) getNameQueueAsignedGenerated = await GetQueueNameConfig(HealthCareStaff.processCode, new { HealthCareStaff.LevelQueueCode, HealthCareStaff.DepartmentId, HealthCareStaff.CountryId, HealthCareStaff.CityId }, (Guid)machineStates.attentionStateTargetId);
            if (string.IsNullOrEmpty(getNameQueueAsignedGenerated.Item1) || string.IsNullOrEmpty(getNameQueuePendingGenerated.Item1)) return RequestResult.ErrorResult($"No existen colas configuradas para el proceso, ciudad e información para el evento de proceso {StateEventProcessEnum.ASIGNATION}");
            string resultEmitMessageAttention = await _messagingFunctions.EmitMessageAsign(getNameQueueAsignedGenerated.Item1, getNameQueuePendingGenerated.Item1, HealthCareStaffId);
            if (string.IsNullOrEmpty(resultEmitMessageAttention)) return RequestResult.ErrorResult($"No se encontró información para la cola de asignación");
            await InsertHistoryAttention(Guid.Parse(resultEmitMessageAttention), (Guid)machineStates.attentionStateTargetId, (Guid)getNameQueueAsignedGenerated.Item2);
            await UpdateMachineStates(Guid.Parse(resultEmitMessageAttention), (Guid)machineStates.attentionStateTargetId, HealthCareStaffId, (Guid)machineStates.healthCareStaffStateId, (Guid)machineStates.patientStateId);
            var resultAttention = await GetAttentionsById(Guid.Parse(resultEmitMessageAttention));
            //await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);
            await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.Monitoring);
            return RequestResult.SuccessRecord(message: "Asignación de atención exitosa", data: resultAttention);
        }
        /* Función que dispara mensaje en cola En Proceso según el proceso seleccionado */
        public async Task<RequestResult> InitAttention(Guid AttentionId) => await EmitAttention(AttentionId, StateEventProcessEnum.INITIATION);
        /* Función que dispara mensaje en cola Finalizado según el proceso seleccionado */
        public async Task<RequestResult> EndAttention(Guid AttentionId) => await EmitAttention(AttentionId, StateEventProcessEnum.ENDING);
        /* Función que cancela la atención */
        public async Task<RequestResult> CancelAttention(Guid AttentionId) => await EmitAttention(AttentionId, StateEventProcessEnum.CANCELLATION);

        /* Función que consulta parametrización a nivel de procesos, ciudad, departamento, pais y linea de negocio*/
        public async Task<(string?, Guid?, Guid?)> GetQueueNameConfig(string processCode, dynamic person, Guid? attentionStateActualId)
        {
            string queueName = string.Empty;
            Guid? GeneratedQueueId = Guid.Empty;
            Guid? BusinessLineLevelValueQueueConf = Guid.Empty;
            if (Enum.TryParse(person.LevelQueueCode, out LevelEnum levelProcess))
            {
                string LevelQueueCode = person.LevelQueueCode;

                IQueryable<GeneratedQueue> query = _context.GeneratedQueues
                    .Include(x => x.ConfigQueue).ThenInclude(x => x.BusinessLineLevelValueQueueConf)
                    .Where(x => x.ConfigQueue.BusinessLineLevelValueQueueConf.Process.Code.Equals(processCode))
                    .Where(x => x.ConfigQueue.BusinessLineLevelValueQueueConf.LevelQueue.Code.Equals(LevelQueueCode))
                    .Where(x => x.ConfigQueue.AttentionStateId.Equals(attentionStateActualId));

                switch (levelProcess)
                {
                    case LevelEnum.PAI:
                        Guid countryPatient = (Guid)person.CountryId;
                        query = query.Where(x => x.ConfigQueue.BusinessLineLevelValueQueueConf.CountryId.Equals(countryPatient));
                        break;
                    case LevelEnum.DEP:
                        Guid departmentPatient = (Guid)person.DepartmentId;
                        query = query.Where(x => x.ConfigQueue.BusinessLineLevelValueQueueConf.DepartmentId.Equals(departmentPatient));
                        break;
                    case LevelEnum.CIU:
                        Guid cityPatient = (Guid)person.CityId;
                        query = query.Where(x => x.ConfigQueue.BusinessLineLevelValueQueueConf.CityId.Equals(cityPatient));
                        break;
                }

                // Ejecutar la consulta de forma asincrónica
                var result = await query.FirstOrDefaultAsync();
                if (result == null)
                    return (string.Empty, Guid.Empty, Guid.Empty);
                queueName = result.Name;
                GeneratedQueueId = result.Id;
                BusinessLineLevelValueQueueConf = result.ConfigQueue.BusinessLineLevelValueQueueConfId;
            }
            return (queueName, GeneratedQueueId, BusinessLineLevelValueQueueConf);
        }

        /* Función que elimina las atenciones y actualiza los estados de los pacientes y personales asistenciales a pendiente  */
        public async Task<AttentionResponse?> GetAttentionsById(Guid AttentionId)
        => await _context.Attentions
                .Where(z => z.Id.Equals(AttentionId))
                .Select(x => new AttentionResponse
                {
                    AttentionId = x.Id,
                    Priority = x.Priority,
                    HealthCareStaff = x.HealthCareStaff != null ? x.HealthCareStaff.Name : "N/A",
                    Patient = x.Patient != null ? x.Patient.Name : "N/A",
                    Process = x.Process != null ? x.Process.Name : string.Empty,
                    City = x.Patient != null && x.Patient.City != null ? x.Patient.City.Name : string.Empty,
                    PatientNum = x.Patient != null ? x.Patient.Identification : string.Empty,
                    Comorbidities = x.Patient != null ? x.Patient.Comorbidities : 0,
                    Age = x.Patient != null && x.Patient.Birthday != null ? CalculatedAge.YearsMonthsDays(Convert.ToDateTime(x.Patient.Birthday)) : string.Empty,
                    State = x.AttentionState != null ? x.AttentionState.Name : string.Empty,
                    Plan = x.Patient != null && x.Patient.Plan != null ? x.Patient.Plan.Name : "N/A",
                    StartDate = x.CreatedAt.HasValue ? x.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                    EndDate = x.EndDate != null ? $"{x.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}" : string.Empty,
                    PatientId = x.Patient != null ? x.Patient.Id : Guid.Empty,
                    HealthCareStaffId = x.HealthCareStaff != null ? x.HealthCareStaff.Id : Guid.Empty,
                })
                .FirstOrDefaultAsync();

        #endregion

        #region Private Methods
        /* Función que guarda la atención  */
        private async Task<Guid> CreateAttention(Guid processId, Guid PatientId, Guid State, string Origin, int priority, Guid BusinessLineLevelValueQueueConfigId)
        {
            var attention = new Attention
            {
                Id = Guid.NewGuid(),
                ProcessId = processId,
                PatientId = PatientId,
                Open = true,
                CreatedAt = DateTime.Now,
                Comments = "Cita creada desde: " + Origin,
                Active = true,
                Priority = priority,
                AttentionStateId = State,
                BusinessLineLevelValueQueueConfigId = BusinessLineLevelValueQueueConfigId

            };
            await _context.AddAsync(attention);
            await _context.SaveChangesAsync();
            return attention.Id;
        }
        /* Función que guarda el historico de atenciones*/
        private async Task InsertHistoryAttention(Guid AttentionId, Guid AttentionStateId, Guid GeneratedQueueId)
        {
            await _context.AddAsync(new AttentionHistory { Id = Guid.NewGuid(), AttentionId = AttentionId, CreatedAt = DateTime.Now, Active = true, AttentionState = AttentionStateId, GeneratedQueueId = GeneratedQueueId });
            await _context.SaveChangesAsync();
        }
        /* Función que guarda la información de la maquina de estados */
        private async Task UpdateMachineStates(Guid AttentionId, Guid AttentionStateId, Guid? HealthCareStaffId, Guid? newStateHealthCareStaff, Guid newStatePatient, bool applyClosed = false, bool applyStart = false)
        {
            var attention = await _context.Attentions.FindAsync(AttentionId);
            if (attention != null && HealthCareStaffId.HasValue)
            {
                attention.AttentionStateId = AttentionStateId;
                attention.HealthCareStaffId = HealthCareStaffId;
                attention.Open = applyClosed ? false : attention.Open;
                attention.StartDate = applyStart ? DateTime.Now : attention.StartDate;
                attention.EndDate = applyClosed ? DateTime.Now : attention.EndDate;
            }
            if (HealthCareStaffId.HasValue)
            {
                var HealthCareStaff = await _context.HealthCareStaffs.FindAsync(HealthCareStaffId);
                if (HealthCareStaff != null)
                {
                    HealthCareStaff.PersonStateId = newStateHealthCareStaff;
                    HealthCareStaff.AvailableAt = applyClosed ? DateTime.Now : HealthCareStaff.AvailableAt;
                }
            }
            var patient = await _context.Patients.FindAsync(attention.PatientId);
            if (patient != null)
                patient.PersonStateId = newStatePatient;
            await _context.SaveChangesAsync();
        }
        /* Función que consulta el proceso por código */
        private async Task<Processor?> GetProcessor(string code) => await _context.Processors.AsNoTracking().Where(x => x.Code == code).SingleOrDefaultAsync();
        private async Task<dynamic?> GetInfoPatient(Guid patientId) => await _context.Patients.AsNoTracking().Include(x => x.City).Select(x => new { x.Id, x.City.DepartmentId, x.City, x.CityId, x.City.Department.CountryId, PlanCode = x.Plan.Code, x.Birthday, x.Comorbidities, LevelQueueCode = x.BusinessLine.LevelQueue.Code }).SingleOrDefaultAsync(x => x.Id == patientId);
        /* Función que consulta el personal asistencial por código */
        private async Task<dynamic?> GetHealthCareStaffById(Guid HealthCareStaffId) => await _context.HealthCareStaffs.AsNoTracking().Select(x => new { x.CityId, x.City.DepartmentId, x.City.Department.CountryId, x.ProcessId, processCode = x.Process.Code, x.Id, LevelQueueCode = x.BusinessLine.LevelQueue.Code }).Where(x => x.Id.Equals(HealthCareStaffId)).SingleOrDefaultAsync();
        /* Función que consulta la atención por Id */
        private async Task<dynamic?> GetAttentionById(Guid AttentionId) => await _context.Attentions.AsNoTracking().Include(x => x.HealthCareStaff).Where(x => x.Id.Equals(AttentionId))
            .Select(x =>
            new
            {
                HealthCareStaffId = x.HealthCareStaffId != null ? x.HealthCareStaffId : Guid.Empty,
                CityId = x.HealthCareStaff != null ? x.HealthCareStaff.CityId : Guid.Empty,
                DepartmentId = x.HealthCareStaff != null ? x.HealthCareStaff.City.DepartmentId : Guid.Empty,
                CountryId = x.HealthCareStaff != null ? x.HealthCareStaff.City.Department.CountryId : Guid.Empty,
                ProcessId = x.HealthCareStaff != null ? x.HealthCareStaff.ProcessId : Guid.Empty,
                PatientId = x.PatientId != null ? x.PatientId : Guid.Empty,
                x.AttentionStateId,
                x.BusinessLineLevelValueQueueConfigId,
                processCode = x.HealthCareStaff != null ? x.HealthCareStaff.Process.Code : string.Empty,
                LevelQueueCode = x.BusinessLineLevelValueQueueConfig.LevelQueue.Code,

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
            (string, Guid?, Guid?) getNameQueueActual = await GetQueueNameConfig(infoAttention.processCode, new { infoAttention.LevelQueueCode, infoAttention.DepartmentId, infoAttention.CountryId, infoAttention.CityId }, (Guid)machineStates.attentionStateActualId);
            (string, Guid?, Guid?) getNameQueueTarget = await GetQueueNameConfig(infoAttention.processCode, new { infoAttention.LevelQueueCode, infoAttention.DepartmentId, infoAttention.CountryId, infoAttention.CityId }, (Guid)machineStates.attentionStateTargetId);
            if (string.IsNullOrEmpty(getNameQueueTarget.Item1) || string.IsNullOrEmpty(getNameQueueActual.Item1)) return RequestResult.ErrorResult($"No existen colas configuradas para el proceso, ciudad e información para el evento de proceso {eventProcess}");
            var sucessMessageMove = await _messagingFunctions.EmitGenericMessage(AttentionId, getNameQueueActual.Item1, getNameQueueTarget.Item1);
            if (!sucessMessageMove) return RequestResult.ErrorResult($"Hubo un problema al momento de consumir la cola");
            await InsertHistoryAttention(AttentionId, (Guid)machineStates.attentionStateTargetId, (Guid)getNameQueueTarget.Item2);
            await UpdateMachineStates(AttentionId, (Guid)machineStates.attentionStateTargetId, infoAttention.HealthCareStaffId, machineStates.healthCareStaffStateId, (Guid)machineStates.patientStateId, eventProcess == StateEventProcessEnum.CANCELLATION || eventProcess == StateEventProcessEnum.ENDING ? true : false, eventProcess == StateEventProcessEnum.CANCELLATION ? true : false);
            var resultAttention = await GetAttentionsById(AttentionId);
            string processResult = await GetAndEmitProcessResult(eventProcess, AttentionId.ToString(), resultAttention);
            await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.Monitoring);
            return RequestResult.SuccessRecord(data: resultAttention, message: processResult);
        }
        /* Función que calcula la prioridad del mensaje con base a la edad del paciente, comorbilidades y plan relacionado */
        private int? calculatedPriority(DateTime birthDate, int? comorbidities, int planRecord)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now < birthDate.AddYears(age))
                age--;
            int? priority = comorbidities;
            if (age >= 18 && age < 60)
                priority += 1;
            else
                priority += 2;
            priority += planRecord;
            return priority;
        }
        /* Función que devuelve resultado string y emite evento de SignalR */
        private async Task<string> GetAndEmitProcessResult(StateEventProcessEnum StateEventProcessEnum, string AttentionId, AttentionResponse resultAttention)
        {
            if (StateEventProcessEnum == StateEventProcessEnum.ENDING || StateEventProcessEnum == StateEventProcessEnum.CANCELLATION)
                await MapDataEndOrCancelAttention();
            //if(StateEventProcessEnum == StateEventProcessEnum.INITIATION)
            //    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);

            switch (StateEventProcessEnum)
            {
                case StateEventProcessEnum.INITIATION:
                    return "Inicio de atención exitosa";

                case StateEventProcessEnum.ENDING:
                    return "Finalización de atención exitosa";

                case StateEventProcessEnum.CANCELLATION:
                    return "Cancelación de atención exitosa";

                default:
                    return "Proceso realizado con éxito";
            }
        }

        /* Función que mapea los datos en el SignalR cuando una atención es cancelado o finalizada */
        private async Task MapDataEndOrCancelAttention()
        {
            var getHealCareStaffAvailable = await _IHealthCareStaffRepository.SearchFirstHealCareStaffAvailable();
            if (getHealCareStaffAvailable?.Data != null)
            {
                var result = await AssignAttention((Guid)getHealCareStaffAvailable.Data);
                /* Si no se asigna automaticamente el estado, enviamos el evento al SignalR para refrescar la pagina */
                //if (!result.Success)
                //    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage);
                //else
                //    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, result.Data);

            }

            //else
            //    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage);
        }
    }
    #endregion
}
