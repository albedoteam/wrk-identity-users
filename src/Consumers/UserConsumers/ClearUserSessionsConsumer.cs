namespace Identity.Business.Users.Consumers.UserConsumers
{
    using System;
    using System.Threading.Tasks;
    using AlbedoTeam.Identity.Contracts.Commands;
    using AlbedoTeam.Identity.Contracts.Events;
    using Db.Abstractions;
    using MassTransit;
    using Microsoft.Extensions.Logging;
    using Services.Accounts;
    using Services.IdentityServers.Abstractions;

    public class ClearUserSessionsConsumer : IConsumer<ClearUserSessions>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<ClearUserSessionsConsumer> _logger;
        private readonly IUserRepository _repository;

        public ClearUserSessionsConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository repository,
            ILogger<ClearUserSessionsConsumer> logger)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _repository = repository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ClearUserSessions> context)
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

            await _identityServer
                .UserProvider(user.Provider)
                .ClearSessions(user.ProviderId);

            await context.Publish<UserSessionsCleared>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                ClearedAt = DateTime.UtcNow,
                context.Message.Reason
            });
        }
    }
}