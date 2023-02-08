﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace IdentityServer4.Contrib.Membership.IdsvrDemo.Models
{
    using System.ComponentModel.DataAnnotations;

    public class EmailInputModel
    {
        [Required]
        [EmailAddress]
        public string EmailAdress { get; set; }

    }
}
