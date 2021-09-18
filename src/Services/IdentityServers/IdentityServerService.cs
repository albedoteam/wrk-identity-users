namespace Identity.Business.Users.Services.IdentityServers
{
    using Abstractions;
    using AlbedoTeam.Identity.Contracts.Common;

    public class IdentityServerService : IIdentityServerService
    {
        private readonly IdentityProviderFactory _factory;

        public IdentityServerService(IdentityProviderFactory factory)
        {
            _factory = factory;
        }

        public IUserProvider UserProvider(Provider provider)
        {
            return _factory.GetUserProvider(provider);
        }
    }
}