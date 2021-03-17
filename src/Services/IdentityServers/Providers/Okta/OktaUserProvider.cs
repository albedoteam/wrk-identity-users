using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Okta.Sdk;
using Okta.Sdk.Configuration;

namespace Identity.Business.Users.Services.IdentityServers.Providers.Okta
{
    public class OktaUserProvider : IUserProvider
    {
        private readonly IdentityServerOptions _identityServerOptions;
        private readonly ILogger<OktaUserProvider> _logger;

        public OktaUserProvider(IdentityServerOptions identityServerOptions, ILogger<OktaUserProvider> logger)
        {
            _identityServerOptions = identityServerOptions;
            _logger = logger;
        }

        public async Task<string> Create(
            string accountName,
            string userTypeProviderId,
            string firstName,
            string lastName,
            string login,
            List<string> groupIds)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var generatedEmail = $"{accountName}-{NewId.NextGuid().ToString()}@albedo.team";

            var user = await client.Users.CreateUserAsync(new CreateUserRequest
            {
                Type = new UserType
                {
                    Id = userTypeProviderId
                },
                Profile = new UserProfile
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = generatedEmail,
                    Login = login
                },
                Credentials = new UserCredentials
                {
                    Password = new PasswordCredential
                    {
                        Value = Guid.NewGuid().ToString()
                    }
                },
                GroupIds = groupIds
            });

            if (user is { }) return user.Id;

            _logger.LogError("User creation failed for username {Login}", login);
            return null;
        }

        public async Task<string> Activate(string userProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var userActivationToken = await client.Users.ActivateUserAsync(userProviderId, false);

            if (userActivationToken is { }) return userActivationToken.ActivationToken;

            _logger.LogError("User activation failed for userProviderId {UserProviderId}", userProviderId);
            return null;
        }

        public async Task Deactivate(string userProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            await client.Users.DeactivateUserAsync(userProviderId);
        }

        public async Task AddGroup(string userProviderId, string groupProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            await client.Groups.AddUserToGroupAsync(groupProviderId, userProviderId);
        }

        public async Task RemoveGroup(string userProviderId, string groupProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            await client.Groups.RemoveUserFromGroupAsync(groupProviderId, userProviderId);
        }

        public async Task<bool> ChangePassword(string userProviderId, string currentPassword, string newPassword)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var userCredentials = await client.Users.ChangePasswordAsync(userProviderId, new ChangePasswordOptions
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });

            if (userCredentials is { }) return true;

            _logger.LogError("Password change failed for userProviderId {UserProviderId}", userProviderId);
            return false;
        }

        public async Task<string> ExpirePasswordAndSetTemporaryOne(string userProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var tempPassword = await client.Users.ExpirePasswordAndGetTemporaryPasswordAsync(userProviderId);
            if (tempPassword is { }) return tempPassword.Password;

            _logger.LogError("Password expiration failed for userProviderId {UserProviderId}", userProviderId);
            return null;
        }

        public async Task<bool> SetPassword(string userProviderId, string newPassword)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var user = await client.Users.GetUserAsync(userProviderId);
            user.Credentials = new UserCredentials
            {
                Password = new PasswordCredential
                {
                    Value = newPassword
                }
            };
            user = await client.Users.PartialUpdateUserAsync(user, userProviderId);

            if (user is { }) return true;

            _logger.LogError("Password set failed for userProviderId {UserProviderId}", userProviderId);
            return false;
        }

        public async Task ClearSessions(string userProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            await client.Users.ClearUserSessionsAsync(userProviderId);
        }

        public async Task Delete(string userProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            await Deactivate(userProviderId);
            await client.Users.DeactivateOrDeleteUserAsync(userProviderId);
        }

        public async Task<bool> Update(string userProviderId,
            string firstName,
            string lastName)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var user = await client.Users.PartialUpdateUserAsync(new User
            {
                Profile = new UserProfile
                {
                    FirstName = firstName,
                    LastName = lastName
                }
            }, userProviderId);

            if (user is { }) return true;

            _logger.LogError("User update failed for userProviderId {UserProviderId}", userProviderId);
            return false;
        }

        public async Task<bool> ChangeUserType(string userProviderId, string userTypeProviderId)
        {
            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _identityServerOptions.ApiUrl,
                Token = _identityServerOptions.ApiKey
            });

            var user = await client.Users.PartialUpdateUserAsync(new User
            {
                Type = new UserType
                {
                    Id = userTypeProviderId
                }
            }, userProviderId);

            if (user is { }) return true;

            _logger.LogError("Change usertype failed for userProviderId {UserProviderId}", userProviderId);
            return false;
        }
    }
}