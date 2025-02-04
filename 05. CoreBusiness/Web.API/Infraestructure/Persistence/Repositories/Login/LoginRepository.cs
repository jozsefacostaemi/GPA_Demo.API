using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Notifications;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Login
{
    public class LoginRepository : ILoginRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmitMessagesRepository _IEmitMessagesRepository;
        private readonly IHealthCareStaffRepository _IHealthCareStaffRepository;
        private readonly NotificationRepository _NotificationRepository;
        private readonly List<string> LstStatesCanNotLoggued = new List<string> { PersonStateEnum.ASIG.ToString(), PersonStateEnum.ENPRO.ToString() };
        public LoginRepository(ApplicationDbContext context, IEmitMessagesRepository IEmitMessagesRepository, IHealthCareStaffRepository IHealthCareStaffRepository, NotificationRepository NotificationRepository)
        {
            _context = context;
            _IEmitMessagesRepository = IEmitMessagesRepository;
            _IHealthCareStaffRepository = IHealthCareStaffRepository;
            _NotificationRepository = NotificationRepository;
        }
        /* Función que valida las credenciales del personal asistencial */
        public async Task<RequestResult> LogIn(string userName, string password)
        {
            Guid? healthCareStaff = await _context.HealthCareStaffs.Where(x => x.UserName.Equals(userName) && x.Password.Equals(password)).Select(x => x.Id).FirstOrDefaultAsync();
            if (healthCareStaff == null || healthCareStaff == Guid.Empty)
                return RequestResult.ErrorResult(message: "Credenciales invalidas");

            var gethealthCareStaff = await _context.HealthCareStaffs.Where(x => x.Id.Equals(healthCareStaff)).FirstOrDefaultAsync();
            if (gethealthCareStaff != null)
            {
                gethealthCareStaff.Loggued = true;
                gethealthCareStaff.AvailableAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            /* Si hay médico disponible, asignamos la cita automaticamente */
            var getHealCareStaffAvailable = await _IHealthCareStaffRepository.SearchFirstHealCareStaffAvailable();
            if (getHealCareStaffAvailable?.Data != null)
            {
                var resultAttention = await _IEmitMessagesRepository.AssignAttention((Guid)getHealCareStaffAvailable.Data);
                /* Si no se asigna automaticamente el estado, enviamos el evento al SignalR para refrescar la pagina */
                if (resultAttention.Success) return resultAttention;
                //else
                //    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, result.Data);
            }
            //else
            //    await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.Monitoring);
            return RequestResult.SuccessResult(message: "Login Exitoso", data: healthCareStaff);
        }
        /* Función que cierre la sesión del personal asistencial */
        public async Task<RequestResult> LogOut(Guid healthCareStaffId)
        {
            var gethealthCareStaff = await _context.HealthCareStaffs.Include(x => x.PersonState).Where(x => x.Id.Equals(healthCareStaffId)).FirstOrDefaultAsync();
            if (gethealthCareStaff == null)
                return RequestResult.SuccessResultNoRecords(message: "El personal asistencial no existe");
            if (LstStatesCanNotLoggued.Contains(gethealthCareStaff.PersonState.Code))
                return RequestResult.SuccessResultNoRecords(message: "El personal asistencial tiene una atención Asignada o En proceso");
            gethealthCareStaff.Loggued = false;
            gethealthCareStaff.AvailableAt = null;
            await _context.SaveChangesAsync();
            await _NotificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.Monitoring);
            return RequestResult.SuccessResult(message: "LogOut Exitoso", data: healthCareStaffId);
        }
    }
}
