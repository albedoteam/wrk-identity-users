using AlbedoTeam.Sdk.DataLayerAccess;
using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Models;

namespace Identity.Business.Users.Db
{
    public class UserRepository : BaseRepositoryWithAccount<User>, IUserRepository
    {
        public UserRepository(
            IBaseRepository<User> baseRepository,
            IHelpersWithAccount<User> helpers) : base(baseRepository, helpers)
        {
        }
    }
}