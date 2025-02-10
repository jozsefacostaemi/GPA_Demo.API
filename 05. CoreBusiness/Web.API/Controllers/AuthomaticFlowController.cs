using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Core.Business.API.Controllers
{
    [Route("authomatic")]
    public class AuthomaticFlowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoginRepository _loginRepository;
        private readonly IEmitMessagesRepository _IEmitMessagesRepository;
        private readonly IHealthCareStaffRepository _IHealthCareStaffRepository;

        public AuthomaticFlowController(ApplicationDbContext context, ILoginRepository loginRepository, IEmitMessagesRepository IEmitMessagesRepository, IHealthCareStaffRepository IHealthCareStaffRepository)
        {
            _context = context;
            _loginRepository = loginRepository;
            _IEmitMessagesRepository = IEmitMessagesRepository;
            _IHealthCareStaffRepository = IHealthCareStaffRepository;
        }

        [HttpGet("EmitAttentions")]
        public async Task<RequestResult> EmitAttentions(int number)
        {
            List<RequestResult> lstResult = new();


            List<string> LstStates = new List<string> { PersonStateEnum.DISP.ToString() };
            List<Patient> patientsPendingAttention = await _context.Patients.Where(x => LstStates.Contains(x.PersonState.Code)).Take(number).ToListAsync();
            List<RequestResult> lstRequestResult = new List<RequestResult>();
            foreach (var dr in patientsPendingAttention)
            {
                var processCode = await _context.Processors
                        .OrderBy(c => Guid.NewGuid())
                        .FirstOrDefaultAsync();

                var result = await _IEmitMessagesRepository.CreateAttention(processCode.Code, dr.Id);
                lstRequestResult.Add(result);
            }
            return RequestResult.SuccessOperation(data:lstRequestResult);         
        }

        [HttpGet("AssigAttentions")]
        public async Task<RequestResult> AssigAttentions(int numberAssigns)
        {
            List<RequestResult> lstResult = new();
            for (int i = 0; i < numberAssigns; i++)
            {
                var processCode = await _context.Processors
                          .OrderBy(c => Guid.NewGuid())
                          .FirstOrDefaultAsync();

                var resulttt = await _IHealthCareStaffRepository.SearchFirstHealCareStaffAvailable(processCode.Code);
                if (resulttt.Success == true && resulttt.Data != null)
                {
                    var resultOperation = await _IEmitMessagesRepository.AssignAttention((Guid)resulttt.Data);
                    lstResult.Add(resultOperation);
                }
            }
            return RequestResult.SuccessOperation(data: lstResult);
        }

        [HttpGet("ProcessAttentions")]
        public async Task<RequestResult> ProcessAttentions(int number)
        {
            List<RequestResult> lstResult = new();

            List<Attention> patientsPendingAttention = await _context.Attentions.Where(x => x.AttentionState.Code.Equals(AttentionStateEnum.ASIG.ToString())).Take(number).ToListAsync();

            foreach (var dr in patientsPendingAttention)
            {
                var resultInProcess = await _IEmitMessagesRepository.StartAttention(dr.Id);
                lstResult.Add(resultInProcess);
            }
            return RequestResult.SuccessOperation(data: lstResult);
        }

        [HttpGet("FinishAttentions")]
        public async Task<RequestResult> FinishAttentions(int number)
        {
            List<RequestResult> lstResult = new();

            List<Attention> patientsPendingAttention = await _context.Attentions.Where(x => x.AttentionState.Code.Equals(AttentionStateEnum.ENPRO.ToString())).Take(number).ToListAsync();

            foreach (var dr in patientsPendingAttention)
            {
                var resultInProcess = await _IEmitMessagesRepository.FinishAttention(dr.Id);
                lstResult.Add(resultInProcess);
            }
            return RequestResult.SuccessOperation(data: lstResult);
        }

        [HttpGet("CancelAttentions")]
        public async Task<RequestResult> CancelAttentions(int number)
        {
            List<RequestResult> lstResult = new();

            List<Attention> patientsPendingAttention = await _context.Attentions.Where(x => x.AttentionState.Code.Equals(AttentionStateEnum.CANC.ToString())).Take(number).ToListAsync();

            foreach (var dr in patientsPendingAttention)
            {
                var resultInProcess = await _IEmitMessagesRepository.FinishAttention(dr.Id);
                lstResult.Add(resultInProcess);
            }
            return RequestResult.SuccessOperation(data: lstResult);
        }

        [HttpGet("LoginAllHealthCareStaff")]
        public async Task<RequestResult> LoginAllHealthCareStaff(int? numberHealthCareStaffs)
        {
            if (numberHealthCareStaffs == null || numberHealthCareStaffs < 0)
                numberHealthCareStaffs = 100000;

            var helathCareStaff = await _context.HealthCareStaffs
                .Where(x => x.Loggued == false).Take((int)numberHealthCareStaffs)
                .ToListAsync();

            List<RequestResult> lstrequest = new List<RequestResult>();

            foreach (var item in helathCareStaff)
            {
                var result = await _loginRepository.LogIn(item.UserName, item.Password);
                lstrequest.Add(result);
            }
            return RequestResult.SuccessRecord(message: "Login realizado con éxito", data: lstrequest);
        }

        [HttpGet("LogOutAllHealthCareStaff")]
        public async Task<RequestResult> LogOutAllHealthCareStaff(int? numberHealthCareStaffs)
        {
            if (numberHealthCareStaffs == null || numberHealthCareStaffs < 0)
                numberHealthCareStaffs = 100000;

            var helathCareStaff = await _context.HealthCareStaffs
                .Where(x => x.Loggued == true).Take((int)numberHealthCareStaffs)
                .ToListAsync();

            List<RequestResult> lstrequest = new List<RequestResult>();

            foreach (var item in helathCareStaff)
            {
                var result = await _loginRepository.LogOut(item.Id);
                lstrequest.Add(result);
            }
            return RequestResult.SuccessRecord(message: "LogOut realizado con éxito", data: lstrequest);
        }

        [HttpGet("CreatePatients")]
        public async Task<RequestResult> CreatePatients(int numberPatients)
        {
            for (int i = 0; i < numberPatients; i++)
            {

                Random objRan = new Random();
                int number = objRan.Next(1, 5000000);
                var city = await _context.Cities
                            .OrderBy(c => Guid.NewGuid())
                            .FirstOrDefaultAsync();

                var plan = await _context.Plans
                            .OrderBy(c => Guid.NewGuid())
                            .FirstOrDefaultAsync();
                Patient objPatient = new Patient();
                objPatient.Id = Guid.NewGuid();
                objPatient.Identification = objRan.Next(649849222, 1102855250).ToString();
                objPatient.Birthday = DateTime.Now;
                objPatient.BusinessLineId = Guid.Parse("DD44C571-4FA5-4133-AECD-062834C93601");
                objPatient.CityId = city.Id;
                objPatient.Comorbidities = objRan.Next(2, 10);
                objPatient.Active = true;
                objPatient.Name = $"Patient {number} - {city.Name} - {plan.Name}";
                objPatient.PlanId = plan.Id;
                objPatient.PersonStateId = Guid.Parse("3FF1ADD3-F87A-4CF7-A50D-EF1DEB0B0E01");
                await _context.AddAsync(objPatient);
                await _context.SaveChangesAsync();

            }
            return RequestResult.SuccessRecord();
        }

        [HttpGet("CreateHealthCareStaffs")]
        public async Task<RequestResult> CreateHealthCareStaffs(int numberHealthCareStaffs)
        {
            for (int i = 0; i < numberHealthCareStaffs; i++)
            {

                Random objRan = new Random();
                int number = objRan.Next(1, 5000000);
                var city = await _context.Cities
                            .OrderBy(c => Guid.NewGuid())
                            .FirstOrDefaultAsync();

                var processor = await _context.Processors
                           .OrderBy(c => Guid.NewGuid())
                           .FirstOrDefaultAsync();

                HealthCareStaff objHealthCareStaff = new();
                objHealthCareStaff.Id = Guid.NewGuid();
                objHealthCareStaff.BusinessLineId = Guid.Parse("DD44C571-4FA5-4133-AECD-062834C93601");
                objHealthCareStaff.AvailableAt = null;
                objHealthCareStaff.CityId = city.Id;
                objHealthCareStaff.Email = $"Dr.{number}.{city.Name}@grupoemi.com";
                objHealthCareStaff.Loggued = false;
                objHealthCareStaff.Name = $"Dr {number} - {city.Name}";
                objHealthCareStaff.ProcessId = processor.Id;
                objHealthCareStaff.Active = true;
                objHealthCareStaff.PersonStateId = Guid.Parse("3FF1ADD3-F87A-4CF7-A50D-EF1DEB0B0E01");
                objHealthCareStaff.UserName = $"user{number}";
                objHealthCareStaff.Password = "1234";
                objHealthCareStaff.Rol = Guid.Parse("D743CAD0-3E44-480D-857B-F04C4A4964A1");
                await _context.AddAsync(objHealthCareStaff);
                await _context.SaveChangesAsync();

            }
            return RequestResult.SuccessRecord();
        }
    }
}
