using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum StateEventProcessEnum
    {
        [EnumMember(Value = "CREATION")]
        CREATED = 1,

        [EnumMember(Value = "ASIGNATION")]
        ASSIGNED = 2,

        [EnumMember(Value = "INITIATION")]
        INPROCESS = 3,

        [EnumMember(Value = "ENDING")]
        FINALIZED = 4,

        [EnumMember(Value = "CANCELLATION")]
        CANCELLED = 5,

        [EnumMember(Value = "AVAILABLE_HEALTHCARESTAFF")]
        AVAILABLE_HEALTHCARESTAFF = 6,

    }
}
