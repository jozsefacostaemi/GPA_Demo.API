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
                case StateEventProcessEnum.CREATION:

                    var creationAttentionState = await GetAttentionState(AttentionStateEnum.PEND);
                    return new StatesMachineResponse { patientStateId = await GetPersonState(PersonStateEnum.ESPASIGPA), attentionStateTargetId = creationAttentionState };

                case StateEventProcessEnum.ASIGNATION:
                    var assignedPersonState = await GetPersonState(PersonStateEnum.ASIG);
                    var assignedAttentionState = await GetAttentionState(AttentionStateEnum.ASIG);
                    var pendingAttentionState = await GetAttentionState(AttentionStateEnum.PEND);
                    return new StatesMachineResponse { patientStateId = assignedPersonState, healthCareStaffStateId = assignedPersonState, attentionStateTargetId = pendingAttentionState, attentionStateActualId = assignedAttentionState};

              
                //TODO
                case StateEventProcessEnum.INITIATION:
                    var inProcessPersonState = await GetPersonState(PersonStateEnum.ENPRO);
                    var inProcessAttentionState = await GetAttentionState(AttentionStateEnum.ENPRO);
                    var assignedAttentionStatee = await GetAttentionState(AttentionStateEnum.ASIG);
                    return new StatesMachineResponse { patientStateId = inProcessPersonState, healthCareStaffStateId = inProcessPersonState, attentionStateTargetId = inProcessAttentionState, attentionStateActualId = assignedAttentionStatee };

                case StateEventProcessEnum.ENDING:

                    var endingPatientState = await GetPersonState(PersonStateEnum.ATEN);
                    var endingHealthCareScaffState = await GetPersonState(PersonStateEnum.DISP);
                    var endingAttentionState = await GetAttentionState(AttentionStateEnum.FINA);
                    var inProcessPreviousAttentionState = await GetAttentionState(AttentionStateEnum.ENPRO);
                    return new StatesMachineResponse { patientStateId = endingPatientState, healthCareStaffStateId = endingHealthCareScaffState, attentionStateTargetId = endingAttentionState, attentionStateActualId = inProcessPreviousAttentionState };

                case StateEventProcessEnum.CANCELLATION:

                    var cancelPatientState = await GetPersonState(PersonStateEnum.CANC);
                    var cancelHealthCareScaffState = await GetPersonState(PersonStateEnum.DISP);
                    var cancelAttentionState = await GetAttentionState(AttentionStateEnum.CANC);
                    return new StatesMachineResponse { patientStateId = cancelPatientState, healthCareStaffStateId = cancelHealthCareScaffState, attentionStateTargetId = cancelAttentionState };
                default:
                    break;
            }
            return null;
        }
        #endregion

        #region Private Methods
        /* Función que consulta estado de Personal asistencial o Paciente */
        private async Task<Guid?> GetPersonState(PersonStateEnum code)
        {
            var personState = await _context.PersonStates.AsNoTracking()
                .Where(x => x.Code.Equals(code.ToString()))
                .FirstOrDefaultAsync();
            return personState?.Id;
        }
        /* Función que consulta estado de la atención*/
        private async Task<Guid?> GetAttentionState(AttentionStateEnum code)
        {
            var attentionState = await _context.AttentionStates.AsNoTracking()
                .Where(x => x.Code.Equals(code.ToString()))
                .FirstOrDefaultAsync();

            return (attentionState?.Id);
        }

        #endregion


    }
}
