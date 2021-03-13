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
    public class RemoveGroupFromUserConsumer : IConsumer<RemoveGroupFromUser>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<RemoveGroupFromUserConsumer> _logger;
        private readonly IUserRepository _userRepository;

        public RemoveGroupFromUserConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository userRepository,
            ILogger<RemoveGroupFromUserConsumer> logger)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<RemoveGroupFromUser> context)
        {
            if (!context.Message.UserId.IsValidObjectId())
            {
                _logger.LogError("The User ID does not have a valid ObjectId format {UserId}", context.Message.UserId);
                return;
            }

            if (!context.Message.GroupId.IsValidObjectId())
            {
                _logger.LogError("The Group ID does not have a valid ObjectId format {GroupId}",
                    context.Message.GroupId);
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

            var contains = user.Groups.Contains(context.Message.GroupId);
            if (!contains)
            {
                _logger.LogInformation("User {UserId} already don't have group {GroupId}",
                    context.Message.UserId, context.Message.GroupId);
                return;
            }

            await _identityServer
                .UserProvider(user.Provider)
                .RemoveGroup(context.Message.UserId, context.Message.GroupId);

            user.Groups.Remove(context.Message.GroupId);

            var updateDefinition = Builders<User>.Update.Combine(
                Builders<User>.Update.Set(u => u.Groups, user.Groups)
            );

            await _userRepository.UpdateById(context.Message.AccountId, context.Message.UserId, updateDefinition);

            await context.Publish<GroupRemovedFromUser>(new
            {
                context.Message.AccountId,
                context.Message.UserId,
                context.Message.GroupId,
                RemovedAt = DateTime.UtcNow
            });
        }
    }
}