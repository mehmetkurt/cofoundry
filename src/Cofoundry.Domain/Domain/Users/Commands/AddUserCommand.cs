﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cofoundry.Domain.CQS;
using Cofoundry.Core.Validation;
using System.Runtime.Serialization;

namespace Cofoundry.Domain
{
    /// <summary>
    /// A generic user creation command for use with Cofoundry users and
    /// other non-Cofoundry users. Does not send any email notifications.
    /// </summary>
    /// <remarks>
    /// Sealed because we should be setting these properties
    /// explicitly and shouldn't allow any possible injection of passwords or
    /// user areas.
    /// </remarks>
    public sealed class AddUserCommand : ICommand, ILoggableCommand, IValidatableObject
    {
        /// <summary>
        /// The first name is not required.
        /// </summary>
        [StringLength(32)]
        public string FirstName { get; set; }

        /// <summary>
        /// The last name is not required.
        /// </summary>
        [StringLength(32)]
        public string LastName { get; set; }

        /// <summary>
        /// The password is required if the user area has AllowPasswordLogin set to 
        /// true, otherwise it should be empty.
        /// </summary>
        [StringLength(300, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [IgnoreDataMember]
        [JsonIgnore]
        public string Password { get; set; }

        /// <summary>
        /// The email address is required if the user area has UseEmailAsUsername 
        /// set to true.
        /// </summary>
        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Please use a valid email address")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        /// <summary>
        /// The username is required if the user area has UseEmailAsUsername set to 
        /// false, otherwise it should be empty and the Email address will be used 
        /// as the username instead.
        /// </summary>
        [StringLength(150)]
        public string Username { get; set; }

        /// <summary>
        /// Indicates whether the user will be prompted to change their password the
        /// next time they log in.
        /// </summary>
        public bool RequirePasswordChange { get; set; }

        /// <summary>
        /// The Cofoundry user system is partitioned into user areas a user
        /// must belong to one of these user areas.
        /// </summary>
        [Required]
        [StringLength(3)]
        public string UserAreaCode { get; set; }

        /// <summary>
        /// The id of the role that this user is assigned to. Either the
        /// RoleId or RoleCode property must be filled in, but not both. The 
        /// role is required and determines the permissions available to the user. 
        /// </summary>
        [PositiveInteger]
        public int? RoleId { get; set; }

        /// <summary>
        /// The code for the role that this user is assigned to. Either the
        /// RoleId or RoleCode property must be filled in, but not both. The 
        /// role is required and determines the permissions available to the user.
        /// </summary>
        [StringLength(3)]
        public string RoleCode { get; set; }

        #region Output

        /// <summary>
        /// The database id of the newly created user. This is set after the command
        /// has been run.
        /// </summary>
        [OutputValue]
        public int OutputUserId { get; set; }

        #endregion

        #region IValidatableObject

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(RoleCode) && !RoleId.HasValue)
            {
                yield return new ValidationResult("Either a role id or role code must be defined.", new string[] { nameof(RoleId) });
            }

            if (!string.IsNullOrWhiteSpace(RoleCode) && RoleId.HasValue)
            {
                yield return new ValidationResult("Either a role id or role code must be defined, not both.", new string[] { nameof(RoleId) });
            }
        }

        #endregion
    }
}
