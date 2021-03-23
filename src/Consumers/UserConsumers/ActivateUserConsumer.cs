using System;
using System.Threading.Tasks;
using AlbedoTeam.Communications.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Events;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Models;
using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.Communications;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class ActivateUserConsumer : IConsumer<ActivateUser>
    {
        private readonly IAccountService _accountService;
        private readonly ICommunicationService _communicationService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<ActivateUserConsumer> _logger;
        private readonly IUserRepository _repository;

        public ActivateUserConsumer(
            IIdentityServerService identityServer,
            IUserRepository repository,
            IAccountService accountService,
            ILogger<ActivateUserConsumer> logger,
            ICommunicationService communicationService)
        {
            _identityServer = identityServer;
            _repository = repository;
            _accountService = accountService;
            _logger = logger;
            _communicationService = communicationService;
        }

        public async Task Consume(ConsumeContext<ActivateUser> context)
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

            if (user.Active)
            {
                _logger.LogWarning("User is already active for id {UserId}", context.Message.Id);
                return;
            }

            var activationToken = await _identityServer
                .UserProvider(user.Provider)
                .Activate(user.ProviderId);

            if (string.IsNullOrWhiteSpace(activationToken))
            {
                _logger.LogError("User activation failed at Provider. UserId {UserId}", context.Message.Id);
                return;
            }

            var update = Builders<User>.Update.Combine(
                Builders<User>.Update.Set(a => a.Active, true),
                Builders<User>.Update.Set(a => a.UpdateReason, context.Message.Reason));

            await _repository.UpdateById(context.Message.AccountId, context.Message.Id, update);

            await SendEmail(context, user);

            await context.Publish<UserActivated>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                ActivatedAt = DateTime.UtcNow,
                context.Message.Reason,
                ActivationToken = activationToken
            });
        }

        private async Task SendEmail(ConsumeContext<ActivateUser> context, User user)
        {
            var rule = await _communicationService.GetCommunicationRule(
                context.Message.AccountId,
                CommunicationEvent.OnUserActivated);

            var redirectUrl = _communicationService.FormatRedirectUrl(rule, context.Message.AccountId);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Usuário Ativado",
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