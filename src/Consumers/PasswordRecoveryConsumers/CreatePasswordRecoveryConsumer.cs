using System;
using System.Linq;
using System.Threading.Tasks;
using AlbedoTeam.Communications.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Events;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Models;
using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.Communications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Business.Users.Consumers.PasswordRecoveryConsumers
{
    public class CreatePasswordRecoveryConsumer : IConsumer<CreatePasswordRecovery>
    {
        private readonly IAccountService _accountService;
        private readonly ICommunicationService _communicationService;
        private readonly ILogger<CreatePasswordRecoveryConsumer> _logger;
        private readonly IPasswordRecoveryRepository _passwordRecoveryRepository;
        private readonly IUserRepository _repository;

        public CreatePasswordRecoveryConsumer(
            IAccountService accountService,
            ILogger<CreatePasswordRecoveryConsumer> logger,
            IUserRepository repository,
            ICommunicationService communicationService,
            IPasswordRecoveryRepository passwordRecoveryRepository)
        {
            _accountService = accountService;
            _logger = logger;
            _repository = repository;
            _communicationService = communicationService;
            _passwordRecoveryRepository = passwordRecoveryRepository;
        }

        public async Task Consume(ConsumeContext<CreatePasswordRecovery> context)
        {
            if (!string.IsNullOrWhiteSpace(context.Message.UserEmail))
            {
                _logger.LogError("The User email is required!");
                return;
            }

            var account = await _accountService.GetAccount(context.Message.AccountId);
            var isAccountValid = account is { } && account.Enabled;
            if (!isAccountValid)
            {
                _logger.LogError("Account invalid for id {AccountId}", context.Message.AccountId);
                return;
            }

            var user = (await _repository.FilterBy(
                context.Message.AccountId,
                u => u.Email == context.Message.UserEmail)).FirstOrDefault();

            if (user is null)
            {
                _logger.LogWarning("User not found for email {UserEmail}", context.Message.UserEmail);
                return;
            }

            var pwdRecovery = await _passwordRecoveryRepository.InsertOne(new PasswordRecovery
            {
                AccountId = context.Message.AccountId,
                UserId = user.Id.ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(3),
                ValidationToken = new Random().Next(0, 999999).ToString("D6")
            });

            await SendEmail(context, user, pwdRecovery.ValidationToken);

            await context.Publish<PasswordRecoveryCreated>(new
            {
                context.Message.AccountId,
                UserId = user.Id.ToString(),
                pwdRecovery.ValidationToken,
                pwdRecovery.CreatedAt,
                ExpiredAt = pwdRecovery.ExpiresAt
            });
        }

        private async Task SendEmail(ConsumeContext<CreatePasswordRecovery> context, User user, string token)
        {
            var rule = await _communicationService.GetCommunicationRule(
                context.Message.AccountId,
                CommunicationEvent.OnPasswordChangeRequested);

            var redirectUrl = _communicationService.FormatRedirectUrl(rule, context.Message.AccountId);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Solicitação de redefinição de senha",
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
                        Key = "token",
                        Value = token
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