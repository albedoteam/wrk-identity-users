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
    using Services.Accounts;
    using Services.Communications;

    public class ResendFirstAccessEmailConsumer : IConsumer<ResendFirstAccessEmail>
    {
        private readonly IAccountService _accountService;
        private readonly ICommunicationService _communicationService;
        private readonly ILogger<ResendFirstAccessEmailConsumer> _logger;
        private readonly IPasswordRecoveryRepository _passwordRecoveryRepository;
        private readonly IUserRepository _userRepository;

        public ResendFirstAccessEmailConsumer(
            ICommunicationService communicationService,
            IAccountService accountService,
            ILogger<ResendFirstAccessEmailConsumer> logger,
            IUserRepository userRepository,
            IPasswordRecoveryRepository passwordRecoveryRepository)
        {
            _communicationService = communicationService;
            _accountService = accountService;
            _logger = logger;
            _userRepository = userRepository;
            _passwordRecoveryRepository = passwordRecoveryRepository;
        }

        public async Task Consume(ConsumeContext<ResendFirstAccessEmail> context)
        {
            if (!context.Message.UserId.IsValidObjectId())
            {
                _logger.LogError("The User ID does not have a valid ObjectId format {UserId}", context.Message.UserId);
                return;
            }

            var account = await _accountService.GetAccount(context.Message.AccountId);
            var isAccountValid = account is { } && account.Enabled;
            if (!isAccountValid)
            {
                _logger.LogError("Account invalid for id {AccountId}", context.Message.AccountId);
                return;
            }

            var user = await _userRepository.FindById(context.Message.AccountId, context.Message.UserId);
            if (user is null)
            {
                _logger.LogError("User not found for id {UserId}", context.Message.UserId);
                return;
            }

            var pwdRecovery = await _passwordRecoveryRepository.InsertOne(new PasswordRecovery
            {
                AccountId = context.Message.AccountId,
                UserId = user.Id.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                ValidationToken = new Random().Next(0, 999999).ToString("D6")
            });

            await SendEmail(context, user, pwdRecovery.ValidationToken);

            await context.Publish<FirstAccessEmailResent>(new
            {
                context.Message.AccountId,
                context.Message.UserId,
                ResentAt = DateTime.UtcNow
            });
        }

        private async Task SendEmail(ConsumeContext<ResendFirstAccessEmail> context, User user, string token)
        {
            var rule = await _communicationService.GetCommunicationRule(
                context.Message.AccountId,
                CommunicationEvent.OnUserCreated);

            var redirectUrl = _communicationService.FormatRedirectUrl(rule, context.Message.AccountId);

            await context.Send<SendMessage>(new
            {
                context.Message.AccountId,
                rule.TemplateId,
                Subject = "Bem vindo!",
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
                        Key = "login",
                        Value = user.Username
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