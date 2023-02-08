// This Source Code Form is subject to the terms of the Mozilla Public
// License, user. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace IdentityServer4.Contrib.Membership
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Helpers;
    using Interfaces;
    using Microsoft.AspNetCore.DataProtection;

    /// <summary>Membership Service that performs some of the MembershipProvider read logic</summary>
    public class MembershipServiceExtended : IMembershipService
    {
        private readonly IMembershipRepository membershipRepository;
        private readonly IMembershipPasswordHasher membershipPasswordHasher;
        private readonly ITimeLimitedDataProtector protector;

        /// <summary>Constructor</summary>
        /// <param name="membershipRepository">Membership Repository</param>
        /// <param name="membershipPasswordHasher">Membership Password Hasher</param>
        public MembershipServiceExtended(IMembershipRepository membershipRepository, IMembershipPasswordHasher membershipPasswordHasher, IDataProtectionProvider protectionProvider)
        {
            this.membershipRepository = membershipRepository.ThrowIfNull(nameof(membershipRepository));
            this.membershipPasswordHasher = membershipPasswordHasher.ThrowIfNull(nameof(membershipPasswordHasher));
            IDataProtectionProvider _protectionProvider = protectionProvider.ThrowIfNull(nameof(protectionProvider));
            this.protector = _protectionProvider.CreateProtector("MembershipPasswordTokenProtector").ToTimeLimitedDataProtector();
        }

        /// <summary>Gets a User by their Unique Identifier</summary>
        /// <param name="userId">User Id</param>
        /// <returns>Membership User</returns>
        public async Task<MembershipUser> GetUserAsync(Guid userId)
        {
            var user = await membershipRepository.FindUserById(userId)
                                                 .ConfigureAwait(false);

            if (user == null) return null;

            return new MembershipUser
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                IsLockedOut = user.IsLockedOut,
                IsApproved = user.IsApproved,
                AccountCreated = user.CreateDate,
                LastActivity = user.LastActivityDate,
                PasswordChanged = user.LastPasswordChangedDate
            };
        }

        /// <summary>Gets a User by their Username</summary>
        /// <param name="username">Username</param>
        /// <returns>Membership User</returns>
        public async Task<MembershipUser> GetUserAsync(string username)
        {
            var user = await membershipRepository.FindUserByUsername(username)
                                                 .ConfigureAwait(false);

            if (user == null) return null;

            return new MembershipUser
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                IsLockedOut = user.IsLockedOut,
                IsApproved = user.IsApproved,
                AccountCreated = user.CreateDate,
                LastActivity = user.LastActivityDate,
                PasswordChanged = user.LastPasswordChangedDate
            };
        }

        /// <summary>Validates the given password is valid for a user</summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if valid, False if not</returns>
        public async Task<bool> ValidateUser(string username, string password)
        {
            // Get the known user password data from the membership repository
            var userSecurity = await membershipRepository.GetUserPassword(username)
                                                         .ConfigureAwait(false);
            if (userSecurity == null) return false;

            // Encrypt the password given using the same encryption data stored against the user
            var encryptedPassword = membershipPasswordHasher.EncryptPassword(password, userSecurity.PasswordFormat, userSecurity.Salt);

            // The Encrypted password should match the password held in the datastore
            var isPasswordCorrect = userSecurity.Password == encryptedPassword;

            // Update our user with any failed password attempts, resets etc.
            if (!isPasswordCorrect ||
                userSecurity.FailedPasswordAttemptCount != 0 ||
                userSecurity.FailedPasswordAnswerAttemptCount != 0)
            {
                await membershipRepository.UpdateUserInfo(username, userSecurity, isPasswordCorrect)
                                          .ConfigureAwait(false);
            }

            var user = await membershipRepository.FindUserByUsername(username).ConfigureAwait(false);

            return isPasswordCorrect && user?.IsApproved == true;
        }

        /// <summary>Updates the password for the given username</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task UpdatePassword(string username, string password)
        {
            // This is how asp.net membership generates the salt
            var rng = RandomNumberGenerator.Create();
            var buf = new byte[16];
            rng.GetBytes(buf);
            var salt = Convert.ToBase64String(buf);

            var encryptedPassword = membershipPasswordHasher.EncryptPassword(password, 1, salt);

            await membershipRepository.UpdatePassword(username, encryptedPassword, salt, 1);
        }

        public async Task<string> GetUsernameAsync(string email)
        {
            return await membershipRepository.FindUserNameByEmail(email).ConfigureAwait(false);
        }

        public async Task<string> GenerateResetPasswordTokenAsync(MembershipUser user, string purpose, double lifetimeInDays)

        {

            string id = Convert.ToString(user.UserId, CultureInfo.InvariantCulture);
            var utcNow = DateTimeOffset.UtcNow;
            var token = $"{purpose}|{id}|{utcNow}|{lifetimeInDays}";

            var protectedToken = this.protector.Protect(token, TimeSpan.FromDays(lifetimeInDays));
            return await Task.FromResult(protectedToken);
        }

        public async Task<bool> ValidateResetPasswordTokenAsync(MembershipUser user, string token, string purpose)
        {
            try
            {
                var unprotectedData = this.protector.Unprotect(token);

                var tokenData = unprotectedData.Split('|', StringSplitOptions.None);
                if (tokenData.Length != 4)
                {
                    return false;
                }

                var tokenLifeSpan = TimeSpan.FromDays(Double.Parse(tokenData[3]));
                var creationTime = DateTimeOffset.Parse(tokenData[2]);
                var expirationTime = creationTime + tokenLifeSpan;
                if (expirationTime < DateTimeOffset.UtcNow)
                {
                    return false;
                }

                var userid = tokenData[1];
                if (!String.Equals(userid, Convert.ToString(user.UserId, CultureInfo.InvariantCulture)))
                {
                    return false;
                }

                var purp = tokenData[0];
                return purp == purpose;

            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Do not leak exception
            }
            return await Task.FromResult(false);
        }

    }
}
