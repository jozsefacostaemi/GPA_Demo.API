using System.Diagnostics;
using Confluent.Kafka;
using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Models;
using Lib.MessageQueues.Functions.Repositories.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.DTOs.Input;
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
        private readonly GetMachineStateValidator _getMachineStateValidator;
        private readonly GetStatesRepository _getStatesRepository;
        private readonly IHealthCareStaffRepository _IHealthCareStaffRepository;
        private readonly IOptions<RabbitMQSettingDTO> _rabbitMQSettings;
        #endregion
        #region Ctor
        public EmitMessageRepository(ApplicationDbContext context, GetMachineStateValidator getMachineStateValidator, GetStatesRepository getStatesRepository, NotificationRepository NotificationRepository, IHealthCareStaffRepository iHealthCareStaffRepository, IOptions<RabbitMQSettingDTO> rabbitMQSettings)
        {
            _context = context;
            _getMachineStateValidator = getMachineStateValidator;
            _getStatesRepository = getStatesRepository;
            _NotificationRepository = NotificationRepository;
            _IHealthCareStaffRepository = iHealthCareStaffRepository;
            _rabbitMQSettings = rabbitMQSettings;
        }
        #endregion
        #region Public Methods
        /* Función que dispara mensaje en cola Pendiente según el proceso seleccionado */
        public async Task<RequestResult> CreateAttention(string ProcessCode, Guid PatientId)
        {
            var Validate = await _getMachineStateValidator.CreationPreconditions(PatientId);
            if (!Validate.Item1) return RequestResult.ErrorResult(message: Validate.Item2);

            StatesMachineResponse? MachineStates = await _getStatesRepository.GetMachineStates(StateEventProcessEnum.CREATED);
            if (MachineStates == null || !MachineStates.Success) return RequestResult.ErrorResult(MachineStates.Message);

            dynamic? Patient = await GetInfoPatient(PatientId);
            if (Patient == null) return RequestResult.ErrorResult(message: "El ID del paciente indicado no existe");

            Processor Process = await GetProcessor(ProcessCode);
            if (Process == null) return RequestResult.ErrorResult(message: "El código de proceso indicado no existe");

            (bool SucessGetQueueNameConfig, string ResultGetQueueName, Guid QueueConfId) = await GetQueueNameConfig(ProcessCode, new { Patient.LevelQueueCode, Patient.CountryId, Patient.DepartmentId, Patient.CityId });
            if (!SucessGetQueueNameConfig) return RequestResult.ErrorResult(message: ResultGetQueueName);

            int Priority = GetPriority((DateTime)Patient.Birthday, Patient.Comorbidities, Patient.PlanCodeNumber);

            Attention NewAttention = await AddAttention(Process.Id, PatientId, MachineStates.NextAttentionStateId, Process.Name, Priority);
            var (sucessProcessEmitAttention, resultProcessEmitAttention) = await TriggerEmitAttention(NewAttention.Id, NewAttention.AttentionStateId, PatientId, MachineStates.NextPatientStateId, QueueConfId, ResultGetQueueName, Priority);
            if (!sucessProcessEmitAttention) return RequestResult.ErrorRecord(message: resultProcessEmitAttention);

            var GetHealCareStaffAvailable = await _IHealthCareStaffRepository.SearchFirstHealCareStaffAvailable(ProcessCode);
            if (GetHealCareStaffAvailable?.Data != null)
                return await AssignAttention((Guid)GetHealCareStaffAvailable.Data);
            var GetAttention = await GetAttentionByIdAsNoTracking(NewAttention.Id);
            await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, GetAttention);
            return RequestResult.SuccessRecord(message: "Creación de atención exitosa", data: GetAttention);
        }
        /* Función que dispara mensaje en cola Asignado según el proceso seleccionado */
        public async Task<RequestResult> AssignAttention(Guid HealthCareStaffId)
        {
            var Validate = await _getMachineStateValidator.AsignationPreconditions(HealthCareStaffId);
            if (!Validate.Item1)
                return RequestResult.ErrorResult(message: Validate.Item2);

            StatesMachineResponse? MachineStates = await _getStatesRepository.GetMachineStates(StateEventProcessEnum.ASSIGNED);
            if (MachineStates == null)
                return RequestResult.ErrorResult($"No existe información para el proceso {StateEventProcessEnum.ASSIGNED}");

            HealthCareStaff? HealthCareStaff = await GetHealthCareStaffById(HealthCareStaffId);
            if (HealthCareStaff == null)
                return RequestResult.ErrorResult($"No existe información para el personal asistencial: {HealthCareStaffId}");

            (bool SucessGetQueueNameConfig, string ResultGetQueueName, Guid QueueConfId) = await GetQueueNameConfig(HealthCareStaff.Process.Code, new { LevelQueueCode = HealthCareStaff.BusinessLine.LevelQueue.Code, HealthCareStaff.City.Department.CountryId, HealthCareStaff.City.DepartmentId, HealthCareStaff.CityId });

            if (!SucessGetQueueNameConfig)
                return RequestResult.ErrorResult(ResultGetQueueName);

            var (SucessConsumeMessage, ResultConsumeMessage, AttentionId) = await ConsumeMessage(ResultGetQueueName, HealthCareStaff.Id, MachineStates);
            if (!SucessConsumeMessage)
                return RequestResult.ErrorResult(ResultConsumeMessage);

            var Attention = await GetAttentionByIdAsNoTracking(AttentionId);
            await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, Attention);
            return RequestResult.SuccessRecord(message: "Asignación de atención exitosa", data: Attention);
        }
        /* Función que dispara mensaje en cola En Proceso según el proceso seleccionado */
        public async Task<RequestResult> StartAttention(Guid AttentionId) => await ProcessAttention(AttentionId, StateEventProcessEnum.INPROCESS);
        /* Función que dispara mensaje en cola Finalizado según el proceso seleccionado */
        public async Task<RequestResult> FinishAttention(Guid AttentionId) => await ProcessAttention(AttentionId, StateEventProcessEnum.FINALIZED);
        /* Función que cancela la atención */
        public async Task<RequestResult> CancelAttention(Guid AttentionId) => await ProcessAttention(AttentionId, StateEventProcessEnum.CANCELLED);
        /* Función que consulta parametrización a nivel de procesos, ciudad, departamento, pais y linea de negocio*/
        public async Task<(bool, string, Guid)> GetQueueNameConfig(string ProcessCode, dynamic? ObjHealthCareStaff)
        {
            string Result = string.Empty;
            Guid? QueueConfId = Guid.Empty;
            if (Enum.TryParse(ObjHealthCareStaff?.LevelQueueCode, out LevelEnum levelProcess))
            {
                string LevelQueueCode = ObjHealthCareStaff.LevelQueueCode;

                IQueryable<GeneratedQueue> query = _context.GeneratedQueues
                    .Include(x => x.QueueConf).ThenInclude(x => x.BusinessLineLevelValueQueueConf)
                    .Where(x => x.QueueConf.BusinessLineLevelValueQueueConf.Process.Code.Equals(ProcessCode))
                    .Where(x => x.QueueConf.BusinessLineLevelValueQueueConf.LevelQueue.Code.Equals(LevelQueueCode));

                switch (levelProcess)
                {
                    case LevelEnum.PAI:
                        Guid countryPatient = ObjHealthCareStaff?.CountryId;
                        query = query.Where(x => x.QueueConf.BusinessLineLevelValueQueueConf.CountryId.Equals(countryPatient));
                        break;
                    case LevelEnum.DEP:
                        Guid departmentPatient = ObjHealthCareStaff?.DepartmentId;
                        query = query.Where(x => x.QueueConf.BusinessLineLevelValueQueueConf.DepartmentId.Equals(departmentPatient));
                        break;
                    case LevelEnum.CIU:
                        Guid cityPatient = ObjHealthCareStaff?.CityId;
                        query = query.Where(x => x.QueueConf.BusinessLineLevelValueQueueConf.CityId.Equals(cityPatient));
                        break;
                }
                var result = await query.FirstOrDefaultAsync();
                if (result == null)
                    return (false, "No existe una cola para la localidad, el proceso y el estado del paciente", Guid.Empty);
                Result = result.Name;
                QueueConfId = result.QueueConfId;
            }
            return (true, Result, (Guid)QueueConfId);
        }
        #endregion
        #region Private Methods
        /* Función que procesa una atención con estado Pendiente y publica mensaje a Encolador de mensajerias */
        private async Task<(bool, string)> TriggerEmitAttention(Guid AttentionId, Guid? AttentionStateId, Guid PatientId, Guid? PatientStateId, Guid? QueueConfId, string QueueName, int Priority)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    Patient Patient = await GetPatientById(PatientId);
                    if (Patient == null) return (false, $"El paciente indicado no existe.");
                    var HistoryTask = AddAttentionHistory(AttentionId, (Guid)AttentionStateId);
                    var UpdatePatientTask = UpdatePatientState(Patient, (Guid)PatientStateId);
                    var ProcessMessageTask = AddProcessMessage(AttentionId, (Guid)QueueConfId);
                    await Task.WhenAll(HistoryTask, UpdatePatientTask, ProcessMessageTask);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await PublishMessageWithErrorHandling(QueueName, AttentionId.ToString(), (byte)Priority, await ProcessMessageTask);
                    return (true, "Atención creada correctamente.");
                }
                catch (Exception ex)
                {
                    // Si algo falla, revertimos los cambios
                    await transaction.RollbackAsync();
                    return (false, $"Error procesando la atención: {ex.Message}");
                }
            }
        }
        /* Función que procesa una atención con estado Asignado y consume mensaje a Encolador de mensajerias */
        public async Task<(bool, string)> TriggerAsignAttention(Guid AttentionId, Guid AttentionStateId, Guid PatientStateId, Guid HealthCareStaffId, Guid HealCareStaffStateId)
        {
            // Empezamos la transacción explícita
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var Attention = await GetAttentionById(AttentionId);
                    if (Attention == null) return (false, $"No existe la atención indicada.{AttentionId}");
                    var Patient = await GetPatientById(Attention.PatientId);
                    if (Patient == null) return (false, $"La atención no tiene relacionado un paciente.");
                    var HealthCareStaff = await GetHealthCareStaffById(HealthCareStaffId);
                    if (HealthCareStaff == null) return (false, $"No existe el médico indicado.");
                    var getProcessMessage = await GetProcessMessage(AttentionId);
                    if (getProcessMessage == null) return (false, $"No ha sido posible crear el procesamiento de la atención.");

                    // Procesos en paralelo que siempre se ejecutan
                    var historyTask = AddAttentionHistory(Attention.Id, AttentionStateId);
                    var updateStaffStateTask = UpdateHealthCareStaffState(HealthCareStaff, HealCareStaffStateId);
                    var updatePatientTask = UpdatePatientState(Patient, PatientStateId);
                    var updateProcessMessgeTask = UpdateProcessMessage(getProcessMessage);
                    var updateAttentionStateTask = UpdateAttentionState(Attention, AttentionStateId, false, HealthCareStaff.Id);
                    await Task.WhenAll(new List<Task> { historyTask, updateStaffStateTask, updatePatientTask, updateProcessMessgeTask, updateAttentionStateTask });
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, "Atención asignada correctamente.");
                }
                catch (Exception ex)
                {
                    // Si hay algún error, revertimos la transacción
                    await transaction.RollbackAsync();
                    return (false, "Hubo un problema al realiza la trasacción de asignación de la cita: " + ex.Message);
                }
            }
        }
        /* Función que procesa la transacción en Estado (En proceso - Finalizado - Cancelado) */
        public async Task<(bool, string)> TriggerProcessAttention(Guid PatientId, Guid PatientStateId, Guid AttentionStateId, Guid HealCareStaffId, Guid HealCareStaffStateId, Guid AttentionId, bool ApplyClosed = false)
        {
            // Empezamos la transacción explícita
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {

                    var patient = await GetPatientById(PatientId);
                    var healcareStaff = await GetHealthCareStaffById(HealCareStaffId);
                    var attention = await GetAttentionById(AttentionId);

                    // Procesos en paralelo, ya que el DbContext se maneja correctamente
                    var historyTask = AddAttentionHistory(attention.Id, AttentionStateId);
                    var updatePatientTask = UpdatePatientState(patient, PatientStateId);
                    var updateStaffStateTask = UpdateHealthCareStaffState(healcareStaff, HealCareStaffStateId);
                    var UpdateAttentionTask = UpdateAttentionState(attention, AttentionStateId, ApplyClosed, null);

                    // Esperamos que todas las tareas terminen
                    await Task.WhenAll(historyTask, updatePatientTask, updateStaffStateTask, UpdateAttentionTask);

                    // Guardamos todos los cambios en la base de datos
                    await _context.SaveChangesAsync();

                    // Confirmamos la transacción
                    await transaction.CommitAsync();

                    return (true, "Atención procesada correctamente");
                }
                catch (Exception ex)
                {
                    // Si hay algún error, revertimos la transacción
                    await transaction.RollbackAsync();
                    return (false, "Hubo un problema al realiza la trasacción: " + ex.Message);
                }
            }
        }
        /* Método que publica mensaje en rabbit y maneja errores */
        private async Task PublishMessageWithErrorHandling(string queueName, string message, byte priority, ProcessMessage processMessage)
        {
            try
            {
                //using (var publisher = new RabbitMQPublisher(_rabbitMQSettings))
                //{
                //var (pubSuccess, pubMessage) = await publisher.PublishMessageAsync(queueName, message, priority);
                //if (pubSuccess)
                //{
                processMessage.Published = true;
                processMessage.PublishedAt = DateTime.Now;
                _context.ProcessMessages.Update(processMessage);
                await _context.SaveChangesAsync();
                //}
                //else
                //    await LogErrorAsync("Error Publish", null, pubMessage);
                //}
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Exception Publish", ex);
            }
        }
        /* Función que permita armar el objeto AttentionHistory */
        private async Task AddAttentionHistory(Guid attentionId, Guid AttentionStateId) => await _context.AttentionHistories.AddAsync(new AttentionHistory { Id = Guid.NewGuid(), AttentionId = attentionId, AttentionState = AttentionStateId, CreatedAt = DateTime.Now });
        /* Función que guarda mensaje de error */
        private async Task LogErrorAsync(string reason, Exception? ex = null, string? message = null)
        {
            var logError = new ProcessMessageErrorLog
            {
                Id = Guid.NewGuid(),
                ErrorMessage = ex.Message != null ? ex.Message : !string.IsNullOrEmpty(message) ? message : "No error messages",
                StackTrace = ex.StackTrace ?? "No stack trace",
                Reason = reason,
                CreatedAt = DateTime.Now
            };

            await _context.ProcessMessageErrorLogs.AddAsync(logError);
            await _context.SaveChangesAsync();
        }
        /* Función que registra el process message */
        private async Task<ProcessMessage> AddProcessMessage(Guid attentionId, Guid QueueConfId)
        {
            var processMessage = new ProcessMessage
            {
                Id = Guid.NewGuid(),
                AttentionId = attentionId,
                QueueConfId = QueueConfId,
                Message = JsonConvert.SerializeObject(attentionId),
                CreatedAt = DateTime.Now,
                Published = false,
                PublishedAt = null
            };
            await _context.ProcessMessages.AddAsync(processMessage);
            return processMessage;
        }
        /* Función que obtiene la información process message */
        private async Task<ProcessMessage> GetProcessMessage(Guid attentionId) => await _context.ProcessMessages.SingleOrDefaultAsync(x => x.AttentionId.Equals(attentionId));
        /* Función que actualiza la información process message */
        private async Task UpdateProcessMessage(ProcessMessage processMessage)
        {
            processMessage.Consumed = true;
            processMessage.ConsumedAt = DateTime.Now;
            _context.ProcessMessages.Update(processMessage);
            _context.Entry(processMessage).State = EntityState.Modified;
        }
        /* Función que actualiza el estado del paciente */
        public async Task UpdatePatientState(Patient patient, Guid patientStateId)
        {
            patient.PersonStateId = patientStateId;
            _context.Patients.Update(patient);
        }
        /* Función que consulta la información del paciente */
        public async Task<Patient?> GetPatientById(Guid patientId) => await _context.Patients.FindAsync(patientId);

        /* Función que actualiza el estado del paciente */
        private async Task UpdateHealthCareStaffState(HealthCareStaff HealthCareStaff, Guid PersonStateId)
        {
            HealthCareStaff.PersonStateId = PersonStateId;
            _context.HealthCareStaffs.Update(HealthCareStaff);
        }
        /* Función que actualiza el estado de la atención */
        private async Task UpdateAttentionState(Attention Attention, Guid stateAttentionId, bool ApplyClosed, Guid? HealthCareStaffId)
        {
            Attention.AttentionStateId = stateAttentionId;
            Attention.HealthCareStaffId = HealthCareStaffId != null ? HealthCareStaffId.Value : Attention.HealthCareStaffId;
            Attention.Open = ApplyClosed ? false : Attention.Open;
            _context.Attentions.Update(Attention);
        }
        /* Función que guarda la atención  */
        private async Task<Attention> AddAttention(Guid ProcessId, Guid? PatientId, Guid? AttentionStateId, string Origin, int Priority)
        {
            var attention = new Attention { Id = Guid.NewGuid(), ProcessId = ProcessId, PatientId = (Guid)PatientId, Open = true, CreatedAt = DateTime.Now, Comments = $"Cita creada desde: + {Origin} a las {DateTime.Now}", Active = true, Priority = Priority, AttentionStateId = (Guid)AttentionStateId };
            await _context.Attentions.AddAsync(attention);
            return attention;
        }
        /* Función que consulta el proceso por código */
        private async Task<Processor> GetProcessor(string code) => await _context.Processors.AsNoTracking().Where(x => x.Code == code).SingleOrDefaultAsync();
        private async Task<dynamic?> GetInfoPatient(Guid patientId) => await _context.Patients.AsNoTracking().Include(x => x.City).Select(x => new { x.Id, x.City.DepartmentId, x.City, x.CityId, x.City.Department.CountryId, PlanCode = x.Plan != null ? x.Plan.Code : string.Empty, PlanCodeNumber = x.Plan != null ? x.Plan.Number : 0, x.Birthday, x.Comorbidities, LevelQueueCode = x.BusinessLine.LevelQueue.Code, }).SingleOrDefaultAsync(x => x.Id == patientId);
        /* Función que consulta el personal asistencial por código */
        public async Task<HealthCareStaff?> GetHealthCareStaffById(Guid healthCareStaffId)
        => await _context.HealthCareStaffs.Where(x => x.Id == healthCareStaffId).Include(x => x.Process).Include(x => x.BusinessLine).ThenInclude(x => x.LevelQueue).Include(x => x.City).ThenInclude(x => x.Department).FirstOrDefaultAsync();

        /* Función que consulta la atención por Id */
        private async Task<Attention?> GetAttentionById(Guid AttentionId) => await _context.Attentions.FindAsync(AttentionId);
        /* Función que consulta la atención por Id */
        private async Task<dynamic?> GetAttentionByIdAsNoTracking(Guid AttentionId) => await _context.Attentions.AsNoTracking().Include(x => x.HealthCareStaff).Where(x => x.Id.Equals(AttentionId))
            .Select(x =>
            new
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
                EndDate = x.EndDate.HasValue ? x.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                PatientId = x.Patient != null ? x.Patient.Id : Guid.Empty,
                HealthCareStaffId = x.HealthCareStaff != null ? x.HealthCareStaff.Id : Guid.Empty,
                CityId = x.HealthCareStaff != null ? x.HealthCareStaff.CityId : Guid.Empty,
                DepartmentId = x.HealthCareStaff != null ? x.HealthCareStaff.City.DepartmentId : Guid.Empty,
                CountryId = x.HealthCareStaff != null ? x.HealthCareStaff.City.Department.CountryId : Guid.Empty,
                ProcessId = x.HealthCareStaff != null ? x.HealthCareStaff.ProcessId : Guid.Empty,
                x.AttentionStateId,
                processCode = x.HealthCareStaff != null ? x.HealthCareStaff.Process.Code : string.Empty,

            }).SingleOrDefaultAsync();
        /* Función que realiza proceso de proceso genericos */
        private async Task<RequestResult> ProcessAttention(Guid AttentionId, StateEventProcessEnum EventProcess)
        {
            Attention? Attention = await GetAttentionById(AttentionId);
            if (Attention == null) return RequestResult.ErrorResult("La atención indicada no existe");

            (bool, string) validate = EventProcess == StateEventProcessEnum.CANCELLED ? await _getMachineStateValidator.CancelationPreconditions(Attention.PatientId, Attention.HealthCareStaffId, AttentionId) : EventProcess == StateEventProcessEnum.FINALIZED ? await _getMachineStateValidator.EndingPreconditions(Attention.PatientId, (Guid)Attention.HealthCareStaffId, AttentionId) : EventProcess == StateEventProcessEnum.INPROCESS ? await _getMachineStateValidator.InitiationPreconditions(Attention.PatientId, (Guid)Attention.HealthCareStaffId, AttentionId) : (false, "El evento indicado no tiene precondiciones configuradas");
            if (!validate.Item1)
                return RequestResult.ErrorResult(message: validate.Item2);

            HealthCareStaff? HealthCareStaff = await GetHealthCareStaffById((Guid)Attention.HealthCareStaffId);
            if (HealthCareStaff == null) return RequestResult.ErrorResult("El médico indicado no existe");

            StatesMachineResponse? MachineStates = await _getStatesRepository.GetMachineStates(EventProcess);
            if (MachineStates == null) return RequestResult.ErrorResult($"No existe información para el evento de proceso {StateEventProcessEnum.CANCELLED}");
            var (SucessTriggerProcessAttention, ResultTriggerProcessAttention) = await TriggerProcessAttention(Attention.PatientId, MachineStates.NextPatientStateId, MachineStates.NextAttentionStateId, HealthCareStaff.Id, (Guid)MachineStates.NextHealthCareStaffStateId, AttentionId, (EventProcess == StateEventProcessEnum.FINALIZED || EventProcess == StateEventProcessEnum.CANCELLED) ? true : false);
            if (!SucessTriggerProcessAttention)
                return RequestResult.ErrorRecord(message: ResultTriggerProcessAttention);
            var resultAttention = await GetAttentionByIdAsNoTracking(AttentionId);
            string processResult = await GetAndEmitProcessResult(EventProcess, AttentionId.ToString(), resultAttention);
            return RequestResult.SuccessRecord(data: resultAttention, message: processResult);
        }
        /* Función que calcula la prioridad del mensaje con base a la edad del paciente, comorbilidades y plan relacionado */
        private int? GetPriority(DateTime birthDate, int? comorbidities, int planRecord)
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
        private async Task<string> GetAndEmitProcessResult(StateEventProcessEnum StateEventProcessEnum, string AttentionId, dynamic resultAttention)
        {
            if (StateEventProcessEnum == StateEventProcessEnum.FINALIZED || StateEventProcessEnum == StateEventProcessEnum.CANCELLED)
                await MapDataEndOrCancelAttention(resultAttention?.processCode);
            switch (StateEventProcessEnum)
            {
                case StateEventProcessEnum.INPROCESS:
                    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);
                    return "Inicio de atención exitosa";

                case StateEventProcessEnum.FINALIZED:
                    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);
                    return "Finalización de atención exitosa";

                case StateEventProcessEnum.CANCELLED:
                    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, resultAttention);
                    return "Cancelación de atención exitosa";

                default:
                    return "Proceso realizado con éxito";
            }
        }
        /* Función que mapea los datos en el SignalR cuando una atención es cancelado o finalizada */
        private async Task MapDataEndOrCancelAttention(string ProcessCode)
        {
            var getHealCareStaffAvailable = await _IHealthCareStaffRepository.SearchFirstHealCareStaffAvailable(ProcessCode);
            if (getHealCareStaffAvailable?.Data != null)
            {
                var result = await AssignAttention((Guid)getHealCareStaffAvailable.Data);
                /* Si no se asigna automaticamente el estado, enviamos el evento al SignalR para refrescar la pagina */
                if (!result.Success)
                    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage);
                else
                    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, result.Data);

            }

            else
                await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage);
        }
        /* Función que permite consumir un mensaje en el orquestador de mensajeria */
        public async Task<(bool, string, Guid)> ConsumeMessage(string queueName, Guid HealthCareStaff, StatesMachineResponse MachineStates)
        {
            //using (var consumer = new RabbitMQConsumer(_rabbitMQSettings))
            //{
            var (success, message, deliveryTag) = await GetFirstMessage(queueName);
            if (success)
            {
                var (sucessProcessAsignAttention, resultProcessAsignAttention) = await TriggerAsignAttention(Guid.Parse(message), (Guid)MachineStates.NextAttentionStateId, (Guid)MachineStates.NextPatientStateId, HealthCareStaff, (Guid)MachineStates.NextHealthCareStaffStateId);
                return (sucessProcessAsignAttention, resultProcessAsignAttention, Guid.Parse(message));
                //if (!sucessProcessAsignAttention)
                //{
                //    var resultNacknowledgeMessageAsync = await consumer.NacknowledgeMessageAsync(deliveryTag);
                //    return (resultNacknowledgeMessageAsync.success, resultNacknowledgeMessageAsync.message, Guid.Parse(message));
                //}
                //else
                //{
                //    var resultAcknowledgeMessageAsync = await consumer.AcknowledgeMessageAsync(deliveryTag);
                //    return (resultAcknowledgeMessageAsync.success, resultAcknowledgeMessageAsync.message, Guid.Parse(message));
                //}
            }
            else
                return (success, message, Guid.Empty);
            //}
            //}
        }
        #endregion

        public async Task<(bool, string, int)> GetFirstMessage(string queueName)
        {
            var result = await (from gq in _context.GeneratedQueues
                                join pm in _context.ProcessMessages on gq.QueueConfId equals pm.QueueConfId
                                join att in _context.Attentions on pm.AttentionId equals att.Id
                                where gq.Name == queueName && att.AttentionState.Code == AttentionStateEnum.PEND.ToString()
                                orderby pm.CreatedAt.Date descending,
                                        pm.CreatedAt.Hour descending,
                                        pm.CreatedAt.Minute descending,
                                        att.Priority descending
                                select new { pm.AttentionId, pm.CreatedAt, att.Priority })
                                .FirstOrDefaultAsync();

            if (result != null)
            {
                return (true, result.AttentionId.ToString(), 0);
            }

            return (false, null, 0);
        }
    }
}