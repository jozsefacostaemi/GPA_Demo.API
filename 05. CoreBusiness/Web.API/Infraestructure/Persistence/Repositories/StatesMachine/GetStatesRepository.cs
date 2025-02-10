using Lib.MessageQueues.Functions.Models;
using Microsoft.EntityFrameworkCore;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.StateMachine
{
    public class GetStatesRepository
    {
        private readonly ApplicationDbContext _context;
        public GetStatesRepository(ApplicationDbContext applicationDbContext)
        {
            _context = applicationDbContext;
        }


        #region Public Methods
        /* Función que consulta los estados para asignar  */
        public async Task<StatesMachineResponse?> GetMachineStates(StateEventProcessEnum state)
        {
            switch (state)
            {
                case StateEventProcessEnum.CREATED:

                    var creationAttentionState = await GetAttentionState(AttentionStateEnum.PEND);
                    return new StatesMachineResponse { Success = true, NextPatientStateId = await GetPersonState(PersonStateEnum.ESPASIGPA), NextAttentionStateId = creationAttentionState };

                case StateEventProcessEnum.ASSIGNED:
                    var AssigPersonStateId = await GetPersonState(PersonStateEnum.ASIG);
                    var assignedAttentionState = await GetAttentionState(AttentionStateEnum.ASIG);
                    var pendingAttentionState = await GetAttentionState(AttentionStateEnum.PEND);
                    return new StatesMachineResponse { Success = true, NextPatientStateId = AssigPersonStateId, NextHealthCareStaffStateId = AssigPersonStateId, NextAttentionStateId = assignedAttentionState, ActualAttentionStateId = pendingAttentionState };

                case StateEventProcessEnum.INPROCESS:
                    var inProcessPersonState = await GetPersonState(PersonStateEnum.ENPRO);
                    var inProcessAttentionState = await GetAttentionState(AttentionStateEnum.ENPRO);
                    var assignedAttentionStatee = await GetAttentionState(AttentionStateEnum.ASIG);
                    return new StatesMachineResponse { Success = true, NextPatientStateId = inProcessPersonState, NextHealthCareStaffStateId = inProcessPersonState, NextAttentionStateId = inProcessAttentionState, ActualAttentionStateId = assignedAttentionStatee };

                case StateEventProcessEnum.FINALIZED:

                    var endingPatientState = await GetPersonState(PersonStateEnum.ATEN);
                    var endingHealthCareScaffState = await GetPersonState(PersonStateEnum.DISP);
                    var endingAttentionState = await GetAttentionState(AttentionStateEnum.FINA);
                    var inProcessPreviousAttentionState = await GetAttentionState(AttentionStateEnum.ENPRO);
                    return new StatesMachineResponse { Success = true, NextPatientStateId = endingPatientState, NextHealthCareStaffStateId = endingHealthCareScaffState, NextAttentionStateId = endingAttentionState, ActualAttentionStateId = inProcessPreviousAttentionState };

                case StateEventProcessEnum.CANCELLED:

                    var cancelPatientState = await GetPersonState(PersonStateEnum.CANC);
                    var cancelHealthCareScaffState = await GetPersonState(PersonStateEnum.DISP);
                    var cancelAttentionState = await GetAttentionState(AttentionStateEnum.CANC);
                    return new StatesMachineResponse { Success = true, NextPatientStateId = cancelPatientState, NextHealthCareStaffStateId = cancelHealthCareScaffState, NextAttentionStateId = cancelAttentionState };
                default:
                    break;
            }
            return new StatesMachineResponse { Message = $"No existe información para el proceso {StateEventProcessEnum.CREATED}", Success = false };
        }
        #endregion

        #region Private Methods
        /* Función que consulta estado de Personal asistencial o Paciente */
        private async Task<Guid> GetPersonState(PersonStateEnum code) => await _context.PersonStates.AsNoTracking().Where(x => x.Code.Equals(code.ToString())).Select(x => x.Id).SingleOrDefaultAsync();
        /* Función que consulta estado de la atención*/
        private async Task<Guid> GetAttentionState(AttentionStateEnum code) =>await _context.AttentionStates.AsNoTracking().Where(x => x.Code.Equals(code.ToString())).Select(x=>x.Id).SingleOrDefaultAsync();


        #endregion


    }
}
