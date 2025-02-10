namespace Lib.MessageQueues.Functions.Models
{
    public class StatesMachineResponse
    {
        public Guid NextAttentionStateId { get; set; }
        public Guid? ActualAttentionStateId { get; set; }
        public Guid NextPatientStateId { get; set; }
        public Guid? NextHealthCareStaffStateId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
