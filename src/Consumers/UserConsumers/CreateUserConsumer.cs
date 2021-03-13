using System.Linq;
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
    public class CreateUserConsumer : IConsumer<CreateUser>
    {
        private readonly IAccountService _accountService;
        private readonly IIdentityServerService _identityServer;
        private readonly IUserMapper _mapper;
        private readonly IUserRepository _repository;

        public CreateUserConsumer(
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

        public async Task Consume(ConsumeContext<CreateUser> context)
        {
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

            var exists = (await _repository.FilterBy(context.Message.AccountId,
                a => a.Username.Equals(context.Message.Username) && !a.IsDeleted)).Any();

            if (exists)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.AlreadyExists,
                    ErrorMessage = "User already exists"
                });
                return;
            }

            var model = _mapper.RequestToModel(context.Message);

            // adjust login 
            var accountName = account.Name.Replace(" ", "-").ToLower();
            var loginOnProvider = $"{model.Username}@{accountName}";

            var userProviderId = await _identityServer
                .UserProvider(context.Message.Provider)
                .Create(
                    accountName,
                    context.Message.UserTypeId,
                    context.Message.FirstName,
                    context.Message.LastName,
                    loginOnProvider,
                    context.Message.Groups);

            if (string.IsNullOrWhiteSpace(userProviderId))
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InternalServerError,
                    ErrorMessage = "It was not possible to create User on Provider"
                });
                return;
            }

            model.ProviderId = userProviderId;

            var user = await _repository.InsertOne(model);
            await context.RespondAsync(_mapper.MapModelToResponse(user));
        }
    }
}