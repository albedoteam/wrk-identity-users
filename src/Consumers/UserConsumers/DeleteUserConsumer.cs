using System.Threading.Tasks;
using AlbedoTeam.Identity.Contracts.Common;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Identity.Contracts.Responses;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Mappers.Abstractions;
using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using MassTransit;

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class DeleteUserConsumer : IConsumer<DeleteUser>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly IUserMapper _mapper;
        private readonly IUserRepository _repository;

        public DeleteUserConsumer(
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

        public async Task Consume(ConsumeContext<DeleteUser> context)
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

            await _identityServer
                .UserProvider(user.Provider)
                .Delete(user.ProviderId);

            await _repository.DeleteById(context.Message.AccountId, context.Message.Id);

            // get "soft-deleted"
            user = await _repository.FindById(context.Message.AccountId, context.Message.Id, true);

            await context.RespondAsync(_mapper.MapModelToResponse(user));
        }
    }
}