// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace IdentityServer4.Contrib.Membership.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using Membership;

    /// <summary>Membership Service</summary>
    public interface IMembershipService
    {
        /// <summary>Gets a User by their Unique Identifier</summary>
        /// <param name="userId">User Id</param>
        /// <returns>Membership User</returns>
        Task<MembershipUser> GetUserAsync(Guid userId);

        /// <summary>Gets a User by their Username</summary>
        /// <param name="username">Username</param>
        /// <returns>Membership User</returns>
        Task<MembershipUser> GetUserAsync(string username);


        /// <summary>Gets a Username by their Email</summary>
        /// <param name="email">email</param>
        /// <returns>Username</returns>
        Task<string> GetUsernameAsync(string email);

        /// <summary>Validates the given password is valid for a user</summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if valid, False if not</returns>
        Task<bool> ValidateUser(string username, string password);

        /// <summary>
        /// Validates the given email identifies an approved user
        /// </summary>
        /// <param name="email"></param>
        /// <returns>True if a user exists and is approved, False if the user does not exists or is not approved</returns>
        Task<bool> ValidateEmailAsync(string email);

        /// <summary>Updates the password for the given username</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task UpdatePassword(string username, string password);
    }
}
