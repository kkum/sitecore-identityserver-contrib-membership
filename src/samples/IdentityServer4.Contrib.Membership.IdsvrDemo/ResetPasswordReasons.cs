using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace IdentityServer4.Contrib.Membership.IdsvrDemo
{
    public enum ResetPasswordReason
    {
        [EnumMember(Value = "fc")]
        FirstConnection,
        [EnumMember(Value = "fp")]
        Forgotten
    }
}
