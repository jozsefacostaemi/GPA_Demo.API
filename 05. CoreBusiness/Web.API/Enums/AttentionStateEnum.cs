using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum AttentionStateEnum
    {
        [EnumMember(Value = "PEND")]
        PEND = 1,

        [EnumMember(Value = "ASIG")]
        ASIG = 2,

        [EnumMember(Value = "ENPRO")]
        ENPRO = 3,

        [EnumMember(Value = "FINA")]
        FINA = 4,

        [EnumMember(Value = "CANC")]
        CANC = 5,

    }
}
