using AlbedoTeam.Identity.Contracts.Common;

namespace Identity.Business.Users.Services.IdentityServers.Abstractions
{
    public interface IIdentityServerService
    {
        IUserProvider UserProvider(Provider provider);
    }
}