using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Login
{
    public class LoginRepository : ILoginRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> LstStatesCanNotLoggued = new List<string> { PersonStateEnum.ASIG.ToString(), PersonStateEnum.ENPRO.ToString() };
        public LoginRepository(ApplicationDbContext context) => _context = context;

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
            return RequestResult.SuccessResult(message: "LogOut Exitoso", data: healthCareStaffId);
        }
}
}
