using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum PersonStateEnum
    {
        [EnumMember(Value = "DISP")]
        DISP = 1,
        [EnumMember(Value = "ENPRO")]
        ENPRO = 2,
        [EnumMember(Value = "DESC")]
        DESC = 3,
        [EnumMember(Value = "ATEN")]
        ATEN = 4,
        [EnumMember(Value = "REC")]
        REC = 5,
        [EnumMember(Value = "ASIG")]
        ASIG = 6,
        [EnumMember(Value = "ESPASIGPA")]
        ESPASIGPA = 7,
        [EnumMember(Value = "VAC")]
        VAC = 8,
        [EnumMember(Value = "CANC")]
        CANC = 9
    }
}
