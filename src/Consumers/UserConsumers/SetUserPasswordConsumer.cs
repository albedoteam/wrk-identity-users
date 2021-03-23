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

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class SetUserPasswordConsumer : IConsumer<SetUserPassword>
    {
        private readonly IAccountService _accountService;
        private readonly ICommunicationService _communicationService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<SetUserPasswordConsumer> _logger;
        private readonly IUserRepository _repository;

        public SetUserPasswordConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository repository,
            ILogger<SetUserPasswordConsumer> logger,
            ICommunicationService communicationService)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _repository = repository;
            _logger = logger;
            _communicationService = communicationService;
        }

        public async Task Consume(ConsumeContext<SetUserPassword> context)
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
                .SetPassword(user.ProviderId, context.Message.Password);

            if (!updated)
            {
                _logger.LogError("Password change for user {UserId} failed", context.Message.Id);
                return;
            }

            await SendEmail(context, user);

            await context.Publish<UserPasswordSetted>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                SettedAt = DateTime.UtcNow
            });
        }

        private async Task SendEmail(ConsumeContext<SetUserPassword> context, User user)
        {
            var rule = await _communicationService.GetCommunicationRule(
                context.Message.AccountId,
                CommunicationEvent.OnPasswordChanged);

            var redirectUrl = _communicationService.FormatRedirectUrl(rule, context.Message.AccountId);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Redefinição de senha realizada",
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