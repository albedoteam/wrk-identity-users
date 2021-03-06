namespace Identity.Business.Users.Consumers.UserConsumers
{
    using System.Linq;
    using System.Threading.Tasks;
    using AlbedoTeam.Identity.Contracts.Common;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using Db.Abstractions;
    using Mappers.Abstractions;
    using MassTransit;
    using Models;
    using MongoDB.Driver;
    using Services.Accounts;
    using Services.IdentityServers.Abstractions;

    public class UpdateUserConsumer : IConsumer<UpdateUser>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly IUserMapper _mapper;
        private readonly IUserRepository _repository;

        public UpdateUserConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserMapper mapper,
            IUserRepository repository)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _mapper = mapper;
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<UpdateUser> context)
        {
            if (!context.Message.Id.IsValidObjectId())
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = "The User ID does not have a valid ObjectId format"
                });

            var account = await _accountService.GetAccount(context.Message.AccountId);
            var isAccountValid = account is { } && account.Enabled;

            if (!isAccountValid)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = $"Account invalid for id {context.Message.AccountId}"
                });
                return;
            }

            var user = await _repository.FindById(context.Message.AccountId, context.Message.Id);
            if (user is null)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.NotFound,
                    ErrorMessage = $"User not found for id {context.Message.Id}"
                });
                return;
            }

            if (!user.Active)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = $"User id {context.Message.Id} is not active to be updated"
                });
                return;
            }

            var exists = (await _repository.FilterBy(
                context.Message.AccountId,
                g => g.Username.Equals(context.Message.Username))).Any();

            if (!exists)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.AlreadyExists,
                    ErrorMessage = $"User with usernname {context.Message.Username} do not exists"
                });
                return;
            }

            var updated = await _identityServer
                .UserProvider(user.Provider)
                .Update(user.ProviderId, context.Message.FirstName, context.Message.LastName);

            if (!updated)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = "Group update failed at Provider"
                });
                return;
            }

            var updateDefinition = Builders<User>.Update.Combine(
                Builders<User>.Update.Set(g => g.Username, context.Message.Username),
                Builders<User>.Update.Set(g => g.FirstName, context.Message.FirstName),
                Builders<User>.Update.Set(g => g.LastName, context.Message.LastName),
                Builders<User>.Update.Set(g => g.Email, context.Message.Email),
                Builders<User>.Update.Set(g => g.CustomProfileFields, context.Message.CustomProfileFields)
            );

            await _repository.UpdateById(context.Message.AccountId, context.Message.Id, updateDefinition);

            // get updated one
            user = await _repository.FindById(context.Message.AccountId, context.Message.Id);

            await context.RespondAsync(_mapper.MapModelToResponse(user));
        }
    }
}