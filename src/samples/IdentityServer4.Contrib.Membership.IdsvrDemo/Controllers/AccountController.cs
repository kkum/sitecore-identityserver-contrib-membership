﻿// This Source Code Form is subject to the terms of the Mozilla Public
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
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    /// <summary>
    /// This is a sample login controller taken from the original IdentityServer4 <a href="https://github.com/IdentityServer/IdentityServer4.Samples/">Samples.</a>
    /// </summary>
    [SecurityHeaders]
    public class AccountController : Controller
    {
        private readonly IMembershipService membershipService;
        private readonly IIdentityServerInteractionService interaction;
        private readonly IClientStore clientStore;

        public AccountController(
            IMembershipService membershipService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore)
        {
            this.membershipService = membershipService;
            this.interaction = interaction;
            this.clientStore = clientStore;
        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var context = await interaction.GetAuthorizationContextAsync(returnUrl);
            var vm = await BuildLoginViewModelAsync(returnUrl, context);
            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            LoginViewModel vm;
            if (string.Equals(button, "validate", StringComparison.Ordinal))
            {
                ModelState.Remove("Username");
                ModelState.Remove("Password");
                if (ModelState.GetFieldValidationState("EmailAddress") == ModelValidationState.Valid)
                {
                    // Check domains ?

                    //find user with the given email
                    var username = await membershipService.GetUsernameAsync(model.EmailAddress);
                    // 1. if the user exists
                    if (null != username)
                    {
                        var user = await membershipService.GetUserAsync(username);
                        if (user.IsNewUser)
                        {
                            // Initialize Password
                            return RedirectToAction("ValidateEmail", "Password", new { purpose = ResetPasswordReason.FirstConnection });
                        }
                        else if (user.IsApproved)
                        {
                            ModelState.ClearValidationState(nameof(LoginInputModel));
                            model.Username = username;
                            vm = await BuildLoginViewModelAsync(model, canLogin: true);
                            return View(vm);
                        }
                    }
                    // 2. if the user does not exists show error view
                    else
                    {
                        ModelState.ClearValidationState(nameof(LoginInputModel));
                        ModelState.AddModelError("", "Please contact your organism.");
                    }
                }
            }

            if (string.Equals(button, "login", StringComparison.Ordinal))
            {
                ModelState.Remove("EmailAddress");
                if (ModelState.IsValid)
                {
                    // validate username/password against in-memory store
                    if (await membershipService.ValidateUser(model.Username, model.Password))
                    {
                        // issue authentication cookie with subject ID and username
                        var user = await membershipService.GetUserAsync(model.Username);

                        await HttpContext.SignInAsync(new IdentityServerUser(user.GetSubjectId())
                        {
                            DisplayName = user.UserName
                        });

                        // make sure the returnUrl is still valid, and if yes - redirect back to authorize endpoint
                        if (interaction.IsValidReturnUrl(model.ReturnUrl))
                        {
                            return RedirectToLocal(model.ReturnUrl);
                        }

                        return Redirect("~/Home/Index");
                    }

                    ModelState.AddModelError("", "Invalid username or password.");
                    vm = await BuildLoginViewModelAsync(model, canLogin: true);
                    return View(vm);
                }
            }

            // something went wrong, show form with error
            vm = await BuildLoginViewModelAsync(model);
            return View(vm);

        }

        async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, AuthorizationRequest context)
        {
            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;
                }
            }

            return new LoginViewModel
            {
                EnableLocalLogin = allowLocal,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
            };
        }

        async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model, bool canLogin = false)
        {
            var context = await interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl, context);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            vm.CanLogin = canLogin;
            return vm;
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public IActionResult Logout(string logoutId)
        {
            var vm = new LogoutViewModel
            {
                LogoutId = logoutId
            };
            if (User == null || !User.Identity.IsAuthenticated)
            {
                return Redirect("~/");
            }
            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            // delete authentication cookie
            await HttpContext.SignOutAsync();

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await interaction.GetLogoutContextAsync(model.LogoutId);

            var vm = new LoggedOutViewModel
            {
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = logout?.ClientId,
                SignOutIframeUrl = logout?.SignOutIFrameUrl
            };

            return View("LoggedOut", vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            if (returnUrl != null)
            {
                returnUrl = UrlEncoder.Default.Encode(returnUrl);
            }
            returnUrl = "/account/externallogincallback?returnUrl=" + returnUrl;

            // start challenge and roundtrip the return URL
            return new ChallengeResult(provider, new AuthenticationProperties
            {
                RedirectUri = returnUrl
            });
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

    }
}
