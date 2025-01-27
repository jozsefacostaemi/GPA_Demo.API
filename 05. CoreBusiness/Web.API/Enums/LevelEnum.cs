using System.Runtime.Serialization;

namespace Web.Core.Business.API.Enums
{
    public enum LevelEnum
    {
        [EnumMember(Value = "CIU")]
        CIU = 1,
        [EnumMember(Value = "DEP")]
        DEP = 2,
        [EnumMember(Value = "PAI")]
        PAI = 3,
        [EnumMember(Value = "PRO")]
        PRO = 4,
    }
}
