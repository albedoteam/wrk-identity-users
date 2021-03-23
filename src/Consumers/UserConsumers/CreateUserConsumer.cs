using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlbedoTeam.Communications.Contracts.Commands;
using AlbedoTeam.Identity.Contracts.Common;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Identity.Contracts.Responses;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Mappers.Abstractions;
using Identity.Business.Users.Models;
using Identity.Business.Users.Services.Accounts;
using Identity.Business.Users.Services.Communications;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using MassTransit;

namespace Identity.Business.Users.Consumers.UserConsumers
{
    public class CreateUserConsumer : IConsumer<CreateUser>
    {
        private readonly IAccountService _accountService;
        private readonly ICommunicationService _communicationService;
        private readonly IRequestClient<GetGroup> _groupClient;
        private readonly IIdentityServerService _identityServer;
        private readonly IUserMapper _mapper;
        private readonly IPasswordRecoveryRepository _passwordRecoveryRepository;
        private readonly IUserRepository _repository;
        private readonly IRequestClient<GetUserType> _userTypeClient;

        public CreateUserConsumer(
            IAccountService accountService,
            IIdentityServerService identityServer,
            IUserMapper mapper,
            IUserRepository repository,
            IRequestClient<GetUserType> userTypeClient,
            IRequestClient<GetGroup> groupClient,
            ICommunicationService communicationService,
            IPasswordRecoveryRepository passwordRecoveryRepository)
        {
            _accountService = accountService;
            _identityServer = identityServer;
            _mapper = mapper;
            _repository = repository;
            _userTypeClient = userTypeClient;
            _groupClient = groupClient;
            _communicationService = communicationService;
            _passwordRecoveryRepository = passwordRecoveryRepository;
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

            var userType = await RequestUserType(context.Message.AccountId, context.Message.UserTypeId);
            if (userType is null)
            {
                await context.RespondAsync<ErrorResponse>(new
                {
                    ErrorType = ErrorType.InvalidOperation,
                    ErrorMessage = $"UserType Id invalid for id {context.Message.UserTypeId}"
                });
                return;
            }

            var groupsOnProvider = new List<string>();
            foreach (var groupId in context.Message.Groups)
            {
                var group = await RequestGroup(context.Message.AccountId, groupId);
                if (group is null)
                {
                    await context.RespondAsync<ErrorResponse>(new
                    {
                        ErrorType = ErrorType.InvalidOperation,
                        ErrorMessage = $"Group Id invalid for id {groupId}"
                    });
                    return;
                }

                groupsOnProvider.Add(group.ProviderId);
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
            var accountName = account.Name.Replace(" ", "_").Replace(".", "_").ToLower();
            var firstNameOnLogin = model.FirstName.Replace(" ", "_").Replace(".", "_").ToLower();
            var lastNameOnLogin = model.LastName.Replace(" ", "_").Replace(".", "_").ToLower();
            model.UsernameAtProvider = $"{firstNameOnLogin}_{lastNameOnLogin}@{accountName}";

            var userProviderId = await _identityServer
                .UserProvider(context.Message.Provider)
                .Create(
                    accountName,
                    userType.ProviderId,
                    context.Message.FirstName,
                    context.Message.LastName,
                    model.UsernameAtProvider,
                    groupsOnProvider);

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

            var pwdRecovery = await _passwordRecoveryRepository.InsertOne(new PasswordRecovery
            {
                AccountId = context.Message.AccountId,
                UserId = user.Id.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                ValidationToken = new Random().Next(0, 999999).ToString("D6")
            });

            await SendEmail(context, user, pwdRecovery.ValidationToken);

            await context.RespondAsync(_mapper.MapModelToResponse(user));
        }

        private async Task<UserTypeResponse> RequestUserType(string accountId, string userTypeId)
        {
            var (userTypeResponse, errorResoponse) = await _userTypeClient.GetResponse<UserTypeResponse, ErrorResponse>(
                new
                {
                    AccountId = accountId,
                    Id = userTypeId,
                    ShowDeleted = false
                });

            if (userTypeResponse.IsCompletedSuccessfully)
            {
                var userType = await userTypeResponse;
                return userType.Message;
            }

            await errorResoponse;
            return null;
        }

        private async Task<GroupResponse> RequestGroup(string accountId, string groupId)
        {
            var (groupResponse, errorResoponse) = await _groupClient.GetResponse<GroupResponse, ErrorResponse>(new
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

        private async Task SendEmail(ConsumeContext<CreateUser> context, User user, string token)
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