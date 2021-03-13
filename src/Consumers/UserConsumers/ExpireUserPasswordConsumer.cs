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
    public class ExpireUserPasswordConsumer : IConsumer<ExpireUserPassword>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<ExpireUserPasswordConsumer> _logger;
        private readonly IUserRepository _repository;

        public ExpireUserPasswordConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository repository,
            ILogger<ExpireUserPasswordConsumer> logger)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _repository = repository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ExpireUserPassword> context)
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

            var temporaryPw = await _identityServer
                .UserProvider(user.Provider)
                .ExpirePasswordAndSetTemporaryOne(context.Message.Id);

            if (string.IsNullOrWhiteSpace(temporaryPw))
            {
                _logger.LogError("Password expiration for user {UserId} failed", context.Message.Id);
                return;
            }

            await context.Publish<UserPasswordExpired>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                ExpiredAt = DateTime.UtcNow,
                context.Message.Reason,
                TemporaryPassword = temporaryPw
            });
        }
    }
}