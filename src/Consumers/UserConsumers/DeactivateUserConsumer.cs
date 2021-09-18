namespace Identity.Business.Users.Consumers.UserConsumers
{
    using System;
    using System.Threading.Tasks;
    using AlbedoTeam.Communications.Contracts.Commands;
    using AlbedoTeam.Identity.Contracts.Commands;
    using AlbedoTeam.Identity.Contracts.Events;
    using Db.Abstractions;
    using MassTransit;
    using Microsoft.Extensions.Logging;
    using Models;
    using MongoDB.Driver;
    using Services.Accounts;
    using Services.Communications;
    using Services.IdentityServers.Abstractions;

    public class DeactivateUserConsumer : IConsumer<DeactivateUser>
    {
        private readonly IAccountService _accountService;
        private readonly ICommunicationService _communicationService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<DeactivateUserConsumer> _logger;
        private readonly IUserRepository _repository;

        public DeactivateUserConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository repository,
            ILogger<DeactivateUserConsumer> logger,
            ICommunicationService communicationService)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _repository = repository;
            _logger = logger;
            _communicationService = communicationService;
        }

        public async Task Consume(ConsumeContext<DeactivateUser> context)
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

            if (!user.Active)
            {
                _logger.LogWarning("User is already inactive for id {UserId}", context.Message.Id);
                return;
            }

            await _identityServer
                .UserProvider(user.Provider)
                .Deactivate(user.ProviderId);

            var update = Builders<User>.Update.Combine(
                Builders<User>.Update.Set(a => a.Active, false),
                Builders<User>.Update.Set(a => a.UpdateReason, context.Message.Reason));

            await _repository.UpdateById(context.Message.AccountId, context.Message.Id, update);

            await SendEmail(context, user);

            await context.Publish<UserDeactivated>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                context.Message.Reason,
                DeactivatedAt = DateTime.UtcNow
            });
        }

        private async Task SendEmail(ConsumeContext<DeactivateUser> context, User user)
        {
            var rule = await _communicationService.GetCommunicationRule(
                context.Message.AccountId,
                CommunicationEvent.OnUserDeactivated);

            var redirectUrl = _communicationService.FormatRedirectUrl(rule, context.Message.AccountId);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Usuário Desativado",
                Destinations = new[]
                {
                    new
                    {
                        Name = user.FirstName,
                        Address = user.Email
                    }
                },
                Parameters = new[]
                {
                    new
                    {
                        Key = "username",
                        Value = user.FirstName
                    },
                    new
                    {
                        Key = "redirectUrl",
                        Value = redirectUrl
                    }
                }
            });
        }
    }
}