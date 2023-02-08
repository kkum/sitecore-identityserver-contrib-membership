using IdentityServer4.Models;
using System.ComponentModel.DataAnnotations;

namespace IdentityServer4.Contrib.Membership.IdsvrDemo.Models
{
    public class ResetPasswordInputModel
    {
        [Required]
        public ResetPasswordReason Purpose { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Confirm password doesn't match, Type again !")]
        public string ConfirmPassword { get; set; }
    }
}
