using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum PlanEnum
    {
        [EnumMember(Value = "BAS")]
        BAS = 1,

        [EnumMember(Value = "STA")]
        STA = 2,

        [EnumMember(Value = "PRE")]
        PRE = 3
    }
}
