﻿using Cofoundry.Core;
using Cofoundry.Domain.Data;
using Cofoundry.Domain.Tests.Shared;
using Cofoundry.Domain.Tests.Shared.Assertions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Cofoundry.Domain.Tests.Integration.Users.Commands
{
    [Collection(nameof(DbDependentFixtureCollection))]
    public class UpdateCurrentUserPasswordCommandHandlerTests
    {
        const string TEST_DOMAIN = "@UpdateCurrentUserPasswordCommandHandlerTests.example.com";
        const string OLD_PASSWORD = "Gr!sh3nk0!";
        const string NEW_PASSWORD = "S3v3rn@ya!";

        private readonly DbDependentTestApplicationFactory _appFactory;

        public UpdateCurrentUserPasswordCommandHandlerTests(
            DbDependentTestApplicationFactory appFactory
            )
        {
            _appFactory = appFactory;
        }

        [Fact]
        public async Task WhenValid_ChangesPassword()
        {
            var username = "WhenValid_ChangesPassword" + TEST_DOMAIN;
            var userId = await AddUserIfNotExistsAsync(username);
            var now = DateTime.UtcNow.AddMinutes(10);
            var updateDate = DateTimeHelper.TruncateMilliseconds(now);
            var command = new UpdateCurrentUserPasswordCommand()
            {
                NewPassword = NEW_PASSWORD,
                OldPassword = OLD_PASSWORD
            };

           
            using (var app = _appFactory.Create())
            {
                app.Mocks.MockDateTime(updateDate);

                var loginService = app.Services.GetService<ILoginService>();
                await loginService.LogAuthenticatedUserInAsync(TestUserArea1.Code, userId, false);

                var repository = app.Services.GetService<IDomainRepository>();
                await repository.ExecuteCommandAsync(command);
            }

            var query = new GetUserLoginInfoIfAuthenticatedQuery()
            {
                UserAreaCode = TestUserArea1.Code,
                Username = username,
                Password = NEW_PASSWORD
            };

            UserLoginInfoAuthenticationResult result;
            User user = null;

            using (var app = _appFactory.Create())
            {
                var repository = app.Services.GetService<IDomainRepository>();
                var dbContext = app.Services.GetService<CofoundryDbContext>();
                result = await repository.ExecuteQueryAsync(query);

                if (result?.User != null)
                {
                    user = await dbContext
                        .Users
                        .AsNoTracking()
                        .SingleOrDefaultAsync(u => u.UserId == result.User.UserId);
                }
            }

            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.User.Should().NotBeNull();
                result.User.RequirePasswordChange.Should().BeFalse();
                result.User.IsEmailConfirmed.Should().BeFalse();
                user.LastPasswordChangeDate.Should().Be(updateDate);
            }
        }

        [Fact]
        public async Task WhenInvalidExistingPassword_Throws()
        {
            var username = "WhenInvalidExistingPassword_ThrowsValidationException" + TEST_DOMAIN;
            var userId = await AddUserIfNotExistsAsync(username);

            var command = new UpdateCurrentUserPasswordCommand()
            {
                NewPassword = OLD_PASSWORD,
                OldPassword = NEW_PASSWORD
            };

            using var app = _appFactory.Create();

            var repository = app.Services.GetService<IDomainRepository>();
            var loginService = app.Services.GetService<ILoginService>();
            await loginService.LogAuthenticatedUserInAsync(TestUserArea1.Code, userId, false);

            await repository
                .Awaiting(r => r.ExecuteCommandAsync(command))
                .Should()
                .ThrowAsync<InvalidCredentialsAuthenticationException>()
                .WithMemberNames(nameof(command.OldPassword));
        }

        [Fact]
        public async Task WhenNotLoggedIn_Throws()
        {
            var command = new UpdateCurrentUserPasswordCommand()
            {
                NewPassword = NEW_PASSWORD,
                OldPassword = OLD_PASSWORD
            };

            using var app = _appFactory.Create();

            var repository = app.Services.GetService<IDomainRepository>();

            await repository
                .Awaiting(r => r.ExecuteCommandAsync(command))
                .Should()
                .ThrowAsync<PermissionValidationFailedException>();
        }

        [Fact]
        public async Task WhenSystemUser_Throws()
        {
            var command = new UpdateCurrentUserPasswordCommand()
            {
                NewPassword = NEW_PASSWORD,
                OldPassword = OLD_PASSWORD
            };

            using var app = _appFactory.Create();

            // elevate to system user account
            var repository = app.Services
                .GetService<IDomainRepository>()
                .WithElevatedPermissions();

            await repository
                .Awaiting(r => r.ExecuteCommandAsync(command))
                .Should()
                .ThrowAsync<EntityNotFoundException<User>>();
        }

        private async Task<int> AddUserIfNotExistsAsync(string username)
        {
            using var app = _appFactory.Create();
            var dbContext = app.Services.GetService<CofoundryDbContext>();

            var userId = await dbContext
                .Users
                .Where(u => u.UserAreaCode == TestUserArea1.Code && u.Username == username)
                .Select(u => u.UserId)
                .SingleOrDefaultAsync();

            if (userId > 0)
            {
                return userId;
            }

            var repository = app.Services
                .GetService<IAdvancedContentRepository>()
                .WithElevatedPermissions();

            var testRole = await repository
                .Roles()
                .GetByCode(TestUserArea1RoleA.Code)
                .AsDetails()
                .ExecuteAsync();

            var command = new AddUserCommand()
            {
                Email = username,
                Password = OLD_PASSWORD,
                FirstName = "Test",
                LastName = "User",
                UserAreaCode = TestUserArea1.Code,
                RoleId = testRole.RoleId,
                RequirePasswordChange = true
            };

            return await repository
                .Users()
                .AddAsync(command);
        }
    }
}
