using Microsoft.EntityFrameworkCore;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Core.Business.API.Infraestructure.Persistence.Validators
{
    public class GetMachineStateValidator
    {
        private readonly ApplicationDbContext _context;
        public GetMachineStateValidator(ApplicationDbContext context)
        {
            _context = context;
        }
        /* Precondiciones para la creación de una atención */
        public async Task<(bool, string)> CreationPreconditions(Guid PatientId)
        {
            var patient = await GetInfoPatient(PatientId);
            if (patient == null) return (false, "El paciente no existe");
            if (patient.PersonStateId != null &&
                (!new List<string> { PersonStateEnum.ATEN.ToString(), PersonStateEnum.CANC.ToString(), PersonStateEnum.VAC.ToString() }.Contains(patient.PersonState.Code)))
                return (false, $"El paciente no tiene un estado valido para crear la atención! \nEstado actual: {patient.PersonState.Name}");
            else
                return (true, "Validación correcta!");
        }
        /* Precondiciones para la asignación de una atención */
        public async Task<(bool, string)> AsignationPreconditions(Guid HealthCareStaffId)
        {
            var HealthCareStaff = await GetInfoHealthCareStaff(HealthCareStaffId);
            if (HealthCareStaff == null) return (false, "El personal asistencial no existe");
            if (HealthCareStaff.PersonState != null && !HealthCareStaff.PersonState.Code.Equals(PersonStateEnum.DISP.ToString()))
                return (false, "El personal asistencial no tiene estado: Disponible!");
            return (true, "Validación correcta!");
        }

        /* Precondiciones para el inicio de una atención */
        public async Task<(bool, string)> InitiationPreconditions(Guid PatientId, Guid HealthCareStaffId, Guid AttentionId)
        {
            var Attention = await GetInfoAttention(AttentionId);
            if (Attention == null) return (false, "La atención no existe");
            if (Attention.AttentionState == null && !Attention.AttentionState.Code.Equals(AttentionStateEnum.ASIG.ToString()))
                return (false, "La atención no tiene estado: Asignado!");

            var patient = await GetInfoPatient(PatientId);
            if (patient == null) return (false, "El paciente no existe");
            if (patient.PersonState != null && !patient.PersonState.Code.Equals(PersonStateEnum.ASIG.ToString()))
                return (false, "El paciente no tiene un estado: Asignado!");

            var HealthCareStaff = await GetInfoHealthCareStaff((Guid)HealthCareStaffId);
            if (HealthCareStaff == null) return (false, "El personal asistencial no existe");
            if (HealthCareStaff.PersonState == null && !HealthCareStaff.PersonState.Code.Equals(PersonStateEnum.DISP.ToString()))
                return (false, "El personal asistencial no tiene estado: Asignado!");
            return (true, "Validación correcta!");
        }

        /* Precondiciones para la finalización de una atención */
        public async Task<(bool, string)> EndingPreconditions(Guid PatientId, Guid HealthCareStaffId, Guid AttentionId)
        {
            var patient = await GetInfoPatient(PatientId);
            if (patient == null) return (false, "El paciente no existe");
            if (patient.PersonState != null && !patient.PersonState.Code.Equals(PersonStateEnum.ENPRO.ToString()))
                return (false, "El paciente no tiene un estado: En proceso!");

            var HealthCareStaff = await GetInfoHealthCareStaff(HealthCareStaffId);
            if (HealthCareStaff == null) return (false, "El personal asistencial no existe");
            if (HealthCareStaff.PersonState != null && !HealthCareStaff.PersonState.Code.Equals(PersonStateEnum.ENPRO.ToString()))
                return (false, "El personal asistencial no tiene estado: En proceso!");

            var Attention = await GetInfoAttention(AttentionId);
            if (Attention == null) return (false, "La atención no existe");
            if (Attention.AttentionState == null && !Attention.AttentionState.Code.Equals(AttentionStateEnum.ENPRO.ToString()))
                return (false, "La atención no tiene estado: En proceso!");
            return (true, "Validación correcta!");
        }
        /* Precondiciones para la cancelación de una atención */
        public async Task<(bool, string)> CancelationPreconditions(Guid PatientId, Guid? HealthCareStaffId, Guid AttentionId)
        {
            var patient = await GetInfoPatient(PatientId);
            if (patient == null) return (false, "El paciente no existe");
            if (patient.PersonState != null &&
                !(new List<string> {
                PersonStateEnum.ESPASIGPA.ToString(),
                PersonStateEnum.ASIG.ToString(),
                PersonStateEnum.ENPRO.ToString() }.Contains(patient.PersonState.Code)))
                return (false, $"El paciente no tiene un estado: A la espera de asignación personal asistencial, Asignado o En Proceso! \nEstado actual: " + patient.PersonState.Name);

            if (HealthCareStaffId != null && HealthCareStaffId != Guid.Empty)
            {
                var HealthCareStaff = await GetInfoHealthCareStaff((Guid)HealthCareStaffId);
                if (HealthCareStaff == null) return (false, "El personal asistencial no existe");
                if (HealthCareStaff.PersonState != null && (!HealthCareStaff.PersonState.Code.Equals(PersonStateEnum.ENPRO.ToString()) || HealthCareStaff.PersonState.Code.Equals(PersonStateEnum.ASIG.ToString())))
                    return (false, $"El personal asistencial no tiene estado Asignado o En proceso! \nEstado actual: {HealthCareStaff.PersonState.Name}");

            }
            var Attention = await GetInfoAttention(AttentionId);
            if (Attention == null) return (false, "La atención no existe");
            if (Attention.AttentionState != null && (
            !Attention.AttentionState.Code.Equals(AttentionStateEnum.ENPRO.ToString()) &&
            !Attention.AttentionState.Code.Equals(AttentionStateEnum.ASIG.ToString())))
                return (false, $"La atención no tiene estado Asignado o En proceso! \nEstado actual: {Attention.AttentionState.Name}");

            return (true, "Validación correcta!");
        }

        #region Private Methods
        /* Función que consulta la información de un paciente */
        private async Task<Patient?> GetInfoPatient(Guid PatientId) => await _context.Patients.AsNoTracking().Include(x => x.PersonState).SingleOrDefaultAsync(x => x.Id.Equals(PatientId));
        /* Función que consulta la información de un personal asistencial */
        private async Task<HealthCareStaff?> GetInfoHealthCareStaff(Guid HealthCareStaffId) => await _context.HealthCareStaffs.AsNoTracking().Include(x => x.PersonState).SingleOrDefaultAsync(x => x.Id.Equals(HealthCareStaffId));
        /* Función que consulta la información de una atención */
        private async Task<Attention?> GetInfoAttention(Guid AttentionId) => await _context.Attentions.AsNoTracking().Include(x => x.AttentionState).SingleOrDefaultAsync(x => x.Id.Equals(AttentionId));
        #endregion
    }
}
