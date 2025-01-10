using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.MessageQueues.Functions.Models
{
    public class MessageInfo
    {
        public string Id { get; set; }    
        public string PatientId { get; set; }
        public string HealthCareStaffId { get; set; }
        public string CityId { get; set; }
        public string ProcessId { get; set; }
    }
}
