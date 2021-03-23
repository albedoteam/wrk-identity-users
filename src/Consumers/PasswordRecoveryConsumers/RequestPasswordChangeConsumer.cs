using System;
using System.Threading.Tasks;
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
    public class RequestPasswordChangeConsumer : IConsumer<RequestPasswordChange>
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<RequestPasswordChangeConsumer> _logger;
        private readonly IUserRepository _repository;
        private readonly ICommunicationService _communicationService;
        private readonly IPasswordRecoveryRepository _passwordRecoveryRepository;

        public RequestPasswordChangeConsumer(
            IAccountService accountService,
            ILogger<RequestPasswordChangeConsumer> logger,
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

        public async Task Consume(ConsumeContext<RequestPasswordChange> context)
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

            var pwdRecovery = await _passwordRecoveryRepository.InsertOne(new PasswordRecovery
            {
                AccountId = context.Message.AccountId,
                UserId = context.Message.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(3),
                ValidationToken = new Random().Next(0, 999999).ToString("D6")
            });

            await _communicationService.SendPasswordChangeRequestedEmail(
                context, 
                user.FirstName,
                user.Email, 
                pwdRecovery.ValidationToken);

            await context.Publish<UserPasswordChangeRequested>(new
            {
                context.Message.AccountId,
                context.Message.Id,
                Token = pwdRecovery.ValidationToken,
                RequestedAt = DateTime.UtcNow
            });
        }
    }
}