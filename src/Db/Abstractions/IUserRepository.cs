using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
using Identity.Business.Users.Models;

namespace Identity.Business.Users.Db.Abstractions
{
    public interface IUserRepository : IBaseRepositoryWithAccount<User>
    {
    }
}