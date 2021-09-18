namespace Identity.Business.Users.Db
{
    using Abstractions;
    using AlbedoTeam.Sdk.DataLayerAccess;
    using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
    using Models;

    public class UserRepository : BaseRepositoryWithAccount<User>, IUserRepository
    {
        public UserRepository(
            IBaseRepository<User> baseRepository,
            IHelpersWithAccount<User> helpers) : base(baseRepository, helpers)
        {
        }
    }
}