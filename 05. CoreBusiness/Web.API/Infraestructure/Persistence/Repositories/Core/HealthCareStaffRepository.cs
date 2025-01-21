using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core
{
    public class HealthCareStaffRepository : IHealthCareStaffRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> ValidChangeStates = new List<string> { PersonStateEnum.REC.ToString(), PersonStateEnum.DISP.ToString() };

        public HealthCareStaffRepository(ApplicationDbContext context) => _context = context;
        public async Task<RequestResult> UpdateStateForHealthCareStaff(Guid HealthCareStaffId, string codeHealthCareStaff)
        {
            if (!ValidChangeStates.Contains(codeHealthCareStaff))
                return RequestResult.SuccessResultNoRecords(message: $"Por favor indique un código de estado valido (DISP ó REC)");


            Guid? stateForHealthCareStaff = await _context.PersonStates
                .Where(x => x.Code.Equals(codeHealthCareStaff))
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            if (stateForHealthCareStaff == null || stateForHealthCareStaff == Guid.Empty)
                return RequestResult.SuccessResultNoRecords(message: $"No existe un estado de atención con código: {codeHealthCareStaff}");
            var HealthCareStaff = await _context.HealthCareStaffs
                .Where(x => x.Id.Equals(HealthCareStaffId))
                .FirstOrDefaultAsync();
            if (HealthCareStaff == null)
                return RequestResult.SuccessResultNoRecords(message: $"No existe un personal asistencial con el id indicado");
            HealthCareStaff.PersonStateId = stateForHealthCareStaff.Value;
            await _context.SaveChangesAsync();
            return RequestResult.SuccessOperation(message: "Estado del personal médico actualizado correctamente");
        }


        public async Task<RequestResult> GetStateByHealthCareStaff(Guid HealthCareStaffId)
        {
            HealthCareStaffResponse? HealthCareStaff = await _context.HealthCareStaffs
                .Where(x => x.Id.Equals(HealthCareStaffId))
                .Select(x => new HealthCareStaffResponse { ActualStateDesc = x.PersonState != null ? x.PersonState.Name : "N/A", ActualStateId = x.PersonStateId, ActualStateCode = x.PersonState != null ? x.PersonState.Code : "Vacio", HealthCareStaffId = x.Id, HealthCareStaffName = x.Name })
                .FirstOrDefaultAsync();
            if (HealthCareStaff == null)
                return RequestResult.SuccessResultNoRecords(message: "No existe el medico con el id indicado");
            return RequestResult.SuccessResult(data: HealthCareStaff);
        }
    }
}
