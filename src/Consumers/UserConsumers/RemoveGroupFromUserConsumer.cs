namespace Identity.Business.Users.Consumers.UserConsumers
{
    using System;
    using System.Threading.Tasks;
    using AlbedoTeam.Identity.Contracts.Commands;
    using AlbedoTeam.Identity.Contracts.Events;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using Db.Abstractions;
    using MassTransit;
    using Microsoft.Extensions.Logging;
    using Models;
    using MongoDB.Driver;
    using Services.Accounts;
    using Services.IdentityServers.Abstractions;

    public class RemoveGroupFromUserConsumer : IConsumer<RemoveGroupFromUser>
    {
        private readonly IAccountService _accountService;
        private readonly IRequestClient<GetGroup> _client;
        private readonly IIdentityServerService _identityServer;
        private readonly ILogger<RemoveGroupFromUserConsumer> _logger;
        private readonly IRequestClient<GetGroup> _client;
        private readonly IUserRepository _userRepository;

        public RemoveGroupFromUserConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserRepository userRepository,
            ILogger<RemoveGroupFromUserConsumer> logger,
            IRequestClient<GetGroup> client)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _userRepository = userRepository;
            _logger = logger;
            _client = client;
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
            
            var group = await RequestGroup(context.Message.AccountId, context.Message.GroupId);
            if (group is null)
            {
                _logger.LogError("Group not found for id {GroupId}", context.Message.GroupId);
                return;
            }

            var group = await RequestGroup(context.Message.AccountId, context.Message.GroupId);
            if (group is null)
            {
                _logger.LogError("Group not found for id {GroupId}", context.Message.GroupId);
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
                .RemoveGroup(user.ProviderId, group.ProviderId);

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

        private async Task<GroupResponse> RequestGroup(string accountId, string groupId)
        {
            var (groupResponse, errorResoponse) = await _client.GetResponse<GroupResponse, ErrorResponse>(new
            {
                AccountId = accountId,
                Id = groupId,
                ShowDeleted = false
            });

            if (groupResponse.IsCompletedSuccessfully)
            {
                var group = await groupResponse;
                return group.Message;
            }

            await errorResoponse;
            return null;
        }
    }
}