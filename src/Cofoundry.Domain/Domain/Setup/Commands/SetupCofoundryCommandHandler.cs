﻿using Cofoundry.Core;
using Cofoundry.Core.Caching;
using Cofoundry.Core.Data;
using Cofoundry.Domain.CQS;
using Cofoundry.Domain.CQS.Internal;
using Cofoundry.Domain.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Cofoundry.Domain.Internal
{
    public class SetupCofoundryCommandHandler
        : ICommandHandler<SetupCofoundryCommand>
        , IIgnorePermissionCheckHandler
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly IQueryExecutor _queryExecutor;
        private readonly CofoundryDbContext _dbContext;
        private readonly ITransactionScopeManager _transactionScopeFactory;
        private readonly UserContextMapper _userContextMapper;
        private readonly IObjectCacheFactory _objectCacheFactory;

        public SetupCofoundryCommandHandler(
            ICommandExecutor commandExecutor,
            IQueryExecutor queryExecutor,
            CofoundryDbContext dbContext,
            ITransactionScopeManager transactionScopeFactory,
            UserContextMapper userContextMapper,
            IObjectCacheFactory objectCacheFactory
            )
        {
            _commandExecutor = commandExecutor;
            _queryExecutor = queryExecutor;
            _dbContext = dbContext;
            _transactionScopeFactory = transactionScopeFactory;
            _userContextMapper = userContextMapper;
            _objectCacheFactory = objectCacheFactory;
        }

        public async Task ExecuteAsync(SetupCofoundryCommand command, IExecutionContext executionContext)
        {
            var settings = await _queryExecutor.ExecuteAsync(new GetSettingsQuery<InternalSettings>());

            if (settings.IsSetup)
            {
                throw new InvalidOperationException("Site is already set up.");
            }

            using (var scope = _transactionScopeFactory.Create(_dbContext))
            {
                var userId = await CreateAdminUser(command);
                var impersonatedUserContext = await GetImpersonatedUserContext(executionContext, userId);

                var settingsCommand = await _queryExecutor.ExecuteAsync(new GetUpdateCommandQuery<UpdateGeneralSiteSettingsCommand>());
                settingsCommand.ApplicationName = command.ApplicationName;
                await _commandExecutor.ExecuteAsync(settingsCommand, impersonatedUserContext);

                // Take the opportunity to break the cache in case any additional install scripts have been run since initialization
                _objectCacheFactory.Clear();

                // Setup Complete
                await _commandExecutor.ExecuteAsync(new MarkAsSetUpCommand(), impersonatedUserContext);

                await scope.CompleteAsync();
            }
        }

        private async Task<ExecutionContext> GetImpersonatedUserContext(IExecutionContext executionContext, int userId)
        {
            var dbUser = await _dbContext
                .Users
                .Include(u => u.Role)
                .FilterByUserArea(CofoundryAdminUserArea.AreaCode)
                .FilterById(userId)
                .SingleOrDefaultAsync();

            EntityNotFoundException.ThrowIfNull(dbUser, userId);
            var impersonatedUserContext = _userContextMapper.Map(dbUser);

            var impersonatedExecutionContext = new ExecutionContext()
            {
                ExecutionDate = executionContext.ExecutionDate,
                UserContext = impersonatedUserContext
            };

            return impersonatedExecutionContext;
        }

        private async Task<int> CreateAdminUser(SetupCofoundryCommand command)
        {
            var newUserCommand = new AddMasterCofoundryUserCommand();
            newUserCommand.Email = command.Email;
            newUserCommand.FirstName = command.FirstName;
            newUserCommand.LastName = command.LastName;
            newUserCommand.Password = command.Password;
            newUserCommand.RequirePasswordChange = command.RequirePasswordChange;
            await _commandExecutor.ExecuteAsync(newUserCommand);

            return newUserCommand.OutputUserId;
        }
    }
}
