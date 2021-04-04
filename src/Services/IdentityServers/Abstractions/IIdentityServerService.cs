namespace Identity.Business.Users.Services.IdentityServers.Abstractions
{
    using AlbedoTeam.Identity.Contracts.Common;

    public interface IIdentityServerService
    {
        IUserProvider UserProvider(Provider provider);
    }
}