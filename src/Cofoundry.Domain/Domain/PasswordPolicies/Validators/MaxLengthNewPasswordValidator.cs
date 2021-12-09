﻿using Cofoundry.Core.Validation;
using Cofoundry.Domain.Internal;
using System;

namespace Cofoundry.Domain
{
    /// <summary>
    /// Validate that a password meets a maximum length. See
    /// https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html#implement-proper-password-strength-controls
    /// for up to date information on best practice password lengths.
    /// </summary>
    /// <inheritdoc/>
    public class MaxLengthNewPasswordValidator
        : INewPasswordValidator
        , INewPasswordValidatorWithConfig<int>
    {
        private static string ERROR_CODE = NewPasswordValidationErrorCodes.AddNamespace("max-length-exceeded");

        /// <summary>
        /// The inclusive maximum length that the password should be. Must be between 6 and 2048
        /// characters.
        /// </summary>
        public int MaxLength { get; private set; }

        public string Criteria => $"Must be {MaxLength} characters or less.";

        public void Configure(int maxLength)
        {
            if (maxLength < PasswordOptions.MIN_LENGTH_BOUNDARY) throw new ArgumentOutOfRangeException(nameof(maxLength));
            if (maxLength > PasswordOptions.MAX_LENGTH_BOUNDARY) throw new ArgumentOutOfRangeException(nameof(maxLength));

            MaxLength = maxLength;
        }

        public ValidationError Validate(INewPasswordValidationContext context)
        {
            if (MaxLength == 0) throw new InvalidOperationException($"{nameof(Configure)} has not been called.");

            if (context.Password.Length > MaxLength)
            {
                return new ValidationError()
                {
                    ErrorCode = ERROR_CODE,
                    Message = $"Password must be {MaxLength} characters or less.",
                    Properties = new string[] { context.PropertyName }
                };
            }

            return null;
        }
    }
}
