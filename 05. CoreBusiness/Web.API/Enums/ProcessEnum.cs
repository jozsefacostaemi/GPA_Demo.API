using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum ProcessEnum
    {
        [EnumMember(Value = "VIDEOCALL")]
        VIDEOCALL = 1,

        [EnumMember(Value = "CALL")]
        CALL = 2,

        [EnumMember(Value = "CHAT")]
        CHAT = 3,

        [EnumMember(Value = "PED")]
        PED = 4,

        [EnumMember(Value = "GINE")]
        GINE = 5
    }
}
