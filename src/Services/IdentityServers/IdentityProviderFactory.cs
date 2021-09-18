namespace Identity.Business.Users.Services.IdentityServers
{
    using System;
    using Abstractions;
    using AlbedoTeam.Identity.Contracts.Common;
    using Providers.Okta;

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