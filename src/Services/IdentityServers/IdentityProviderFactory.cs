using System;
using AlbedoTeam.Identity.Contracts.Common;
using Identity.Business.Users.Services.IdentityServers.Abstractions;
using Identity.Business.Users.Services.IdentityServers.Providers.Okta;

namespace Identity.Business.Users.Services.IdentityServers
{
    public class IdentityProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public IdentityProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IUserProvider GetUserProvider(Provider provider)
        {
            return provider switch
            {
                Provider.Okta => (IUserProvider) _serviceProvider.GetService(typeof(OktaUserProvider)),
                _ => throw new NotImplementedException()
            };
        }
    }
}