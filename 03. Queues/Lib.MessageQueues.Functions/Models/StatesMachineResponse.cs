using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.MessageQueues.Functions.Models
{
    public class StatesMachineResponse
    {
        public Guid? attentionStateTargetId { get; set; }
        public Guid? attentionStateActualId { get; set; }
        public Guid? patientStateId { get; set; }
        public Guid? healthCareStaffStateId { get; set; }
    }
}
