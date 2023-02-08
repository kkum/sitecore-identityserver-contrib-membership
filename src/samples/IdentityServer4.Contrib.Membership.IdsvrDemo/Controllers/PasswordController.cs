// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace IdentityServer4.Contrib.Membership.IdsvrDemo.Controllers
{
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Helpers;
    using IdentityServer4.Models;
    using Interfaces;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Services;
    using Stores;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.AspNetCore.Authorization;
    using static IdentityServer4.Events.TokenIssuedSuccessEvent;

    /// <summary>
    /// This is a sample login controller taken from the original IdentityServer4 <a href="https://github.com/IdentityServer/IdentityServer4.Samples/">Samples.</a>
    /// </summary>
    [SecurityHeaders]
    public class PasswordController : Controller
    {
        private readonly IMembershipService membershipService;
        private readonly IIdentityServerInteractionService interaction;
        private readonly IClientStore clientStore;
        private readonly IConfiguration configuration;
        private readonly IDataProtector protector;
        private readonly bool enablePasswordReset;

        public PasswordController(
            IMembershipService membershipService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IDataProtectionProvider provider,
            IConfiguration configuration)
        {
            this.membershipService = membershipService;
            this.interaction = interaction;
            this.clientStore = clientStore;
            this.configuration = configuration;
            this.protector = provider.CreateProtector("UserDataProtector");
            this.enablePasswordReset = (bool)configuration.GetValue(typeof(bool), "MembershipProvider:AllowPasswordReset", false);
        }

        /// <summary>
        /// Show Password Initialization page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateEmail(string purpose, string tokenExpired = "False")
        {
            ResetPasswordReason reason;
            if (Enum.TryParse<ResetPasswordReason>(purpose, true, out reason))
            {
                var vm = await BuildEmailViewModelAsync(reason, tokenExpired.ToLower() == "true");
                return View(vm);
            }
            else
            {
                return View("Error");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateEmail(EmailInputModel model)
        {
            EmailViewModel vm;
            if (ModelState.IsValid)
            {
                var username = await membershipService.GetUsernameAsync(model.EmailAddress);
                // 1. if the user exists
                if (null != username)
                {
                    var user = await membershipService.GetUserAsync(username);
                    if ((user.IsNewUser && model.Purpose.Equals(ResetPasswordReason.FirstConnection))
                     || (user.IsApproved && model.Purpose.Equals(ResetPasswordReason.Forgotten)))
                    {
                        var tokenValidity = (int)configuration.GetValue(typeof(int), $"MembershipProvider:ResetTokenLifeSpan:{model.Purpose}", (int)1);
                        var code = await membershipService.GenerateResetPasswordTokenAsync(user, model.Purpose.ToString(), tokenValidity);
                        var userId = Convert.ToBase64String(protector.Protect(user.UserId.ToByteArray()));

                        var link = Url.Action(nameof(Reset), "Password", new { userId, code }, Request.Scheme, Request.Host.ToString());
                        // Send mail with Link to model.EmailAddress
                        ModelState.AddModelError("link", link);
                        //return RedirectToAction("EmailValidated", "Password", new { purpose = model.Purpose });
                    }
                }
            }
            ModelState.ClearValidationState(nameof(LoginInputModel));
            ModelState.AddModelError("", "Please contact your organism.");
            vm = await BuildEmailViewModelAsync(model);
            return View(vm);
        }


        async Task<EmailViewModel> BuildEmailViewModelAsync(ResetPasswordReason purpose, bool tokenExpired)
        {
            var vm = new EmailViewModel
            {
                EnablePasswordReset = this.enablePasswordReset,
                Purpose = purpose,
                TokenExpired = tokenExpired,
            };
            return await Task.FromResult(vm);
        }
        async Task<EmailViewModel> BuildEmailViewModelAsync(EmailInputModel model)
        {
            var vm = await BuildEmailViewModelAsync(model.Purpose, model.TokenExpired);
            vm.EmailAddress = model.EmailAddress;
            return vm;

        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Reset(string userId, string code)
        {
            var id = new Guid(protector.Unprotect(Convert.FromBase64String(userId)));
            var user = await membershipService.GetUserAsync(id);
            if (user == null)
            {
                return View("Error");
            }

            var purpose = user.IsNewUser ? ResetPasswordReason.FirstConnection : ResetPasswordReason.Forgotten;
            var isTokenValid = await membershipService.ValidateResetPasswordTokenAsync(user, code, purpose.ToString());
            if (isTokenValid)
            {
                var vm = new ResetPasswordViewModel
                {
                    userId = userId,
                    EmailAddress = user.Email,
                    Purpose = purpose,
                    EnablePasswordReset = this.enablePasswordReset
                };
                return View(vm);
            }
            else
            {
                return RedirectToAction("ValidateEmail", new { purpose, tokenExpired = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(ResetPasswordInputModel model)
        {
            if (!ModelState.IsValid)
            {
                var username = await membershipService.GetUsernameAsync(model.EmailAddress);
                await membershipService.UpdatePassword(username, model.Password);

                return RedirectToAction("Updated");
            }
            var vm = await BuildResetPasswordViewModel(model);
            return View(vm);
        }

        async Task<ResetPasswordViewModel> BuildResetPasswordViewModel(ResetPasswordInputModel model)
        {
            var vm = new ResetPasswordViewModel
            {
                EmailAddress = model.EmailAddress,
                Purpose = model.Purpose,
            };
            return vm;
        }

        public IActionResult EmailValidated() => View();



    }

}
