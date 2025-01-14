using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum StateEventProcessEnum
    {
        [EnumMember(Value = "CREATION")]
        CREATION = 1,

        [EnumMember(Value = "ASIGNATION")]
        ASIGNATION = 2,

        [EnumMember(Value = "INITIATION")]
        INITIATION = 3,

        [EnumMember(Value = "ENDING")]
        ENDING = 4,

        [EnumMember(Value = "CANCELLATION")]
        CANCELLATION = 5,

        [EnumMember(Value = "AVAILABLE_HEALTHCARESTAFF")]
        AVAILABLE_HEALTHCARESTAFF = 6,

    }
}
