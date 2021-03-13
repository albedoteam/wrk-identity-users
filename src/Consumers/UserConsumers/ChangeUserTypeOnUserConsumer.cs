using System;
using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Events;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Models;
using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class ChangeUserTypeOnUserConsumer : IConsumer<ChangeUserTypeOnUser>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<ChangeUserTypeOnUserConsumer> _logger;

        private readonly IUserRepository _userRepository;
        // private readonly IUserTypeRepository _userTypeRepository;

        public ChangeUserTypeOnUserConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository userRepository,
            // IUserTypeRepository userTypeRepository,
            ILogger<ChangeUserTypeOnUserConsumer> logger)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _userRepository = userRepository;
            // _userTypeRepository = userTypeRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ChangeUserTypeOnUser> context)
        {
            if (!context.Message.UserId.IsValidObjectId())
            {
                _logger.LogError("The User ID does not have a valid ObjectId format {UserId}", context.Message.UserId);
                return;
            }

            if (!context.Message.UserTypeId.IsValidObjectId())
            {
                _logger.LogError("The UserType ID does not have a valid ObjectId format {UserTypeId}",
                    context.Message.UserTypeId);
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

            // var userType = await _userTypeRepository.FindById(context.Message.AccountId, context.Message.UserTypeId);
            // if (userType is null)
            // {
            //     _logger.LogError("UserType not found for id {UserTypeId}", context.Message.UserTypeId);
            //     return;
            // }

            var updated = await _identityServer
                .UserProvider(user.Provider)
                .ChangeUserType(context.Message.UserId, context.Message.UserTypeId);

            if (!updated)
            {
                _logger.LogError("Change usertype {UserTypeId} for user {UserId} failed",
                    context.Message.UserTypeId, context.Message.UserId);
                return;
            }

            var updateDefinition = Builders<User>.Update.Combine(
                Builders<User>.Update.Set(u => u.UserTypeId, context.Message.UserTypeId)
            );

            await _userRepository.UpdateById(context.Message.AccountId, context.Message.UserId, updateDefinition);

            await context.Publish<UserTypeChangedOnUser>(new
            {
                context.Message.AccountId,
                context.Message.UserId,
                context.Message.UserTypeId,
                ChangedAt = DateTime.UtcNow
            });
        }
    }
}