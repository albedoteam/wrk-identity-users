using System;
using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Events;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class ChangeUserPasswordConsumer : IConsumer<ChangeUserPassword>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<ChangeUserPasswordConsumer> _logger;
        private readonly IUserRepository _repository;

        public ChangeUserPasswordConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository repository,
            ILogger<ChangeUserPasswordConsumer> logger)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _repository = repository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ChangeUserPassword> context)
        {
            if (!context.Message.Id.IsValidObjectId())
            {
                _logger.LogError("The User ID does not have a valid ObjectId format {UserId}", context.Message.Id);
                return;
            }

            var account = await _accountService.GetAccount(context.Message.AccountId);
            var isAccountValid = account is { } && account.Enabled;
            if (!isAccountValid)
            {
                _logger.LogError("Account invalid for id {AccountId}", context.Message.AccountId);
                return;
            }

            var user = await _repository.FindById(context.Message.AccountId, context.Message.Id);
            if (user is null)
            {
                _logger.LogError("User not found for id {UserId}", context.Message.Id);
                return;
            }

            var updated = await _identityServer
                .UserProvider(user.Provider)
                .ChangePassword(user.ProviderId, context.Message.OldPassword, context.Message.NewPassword);

            if (!updated)
            {
                _logger.LogError("Password change for user {UserId} failed", context.Message.Id);
                return;
            }

            await context.Publish<UserPasswordChanged>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                ChangedAt = DateTime.UtcNow
            });
        }
    }
}