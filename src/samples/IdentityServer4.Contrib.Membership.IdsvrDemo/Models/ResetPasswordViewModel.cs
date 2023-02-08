using IdentityServer4.Models;
using System.ComponentModel.DataAnnotations;

namespace IdentityServer4.Contrib.Membership.IdsvrDemo.Models
{
    public class ResetPasswordViewModel : ResetPasswordInputModel
    {
        public string userId{get;set;}
        public bool EnablePasswordReset { get; set; }
    }
}
