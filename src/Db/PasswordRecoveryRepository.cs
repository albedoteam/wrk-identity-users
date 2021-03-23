using AlbedoTeam.Sdk.DataLayerAccess;
using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
using Identity.Business.Users.Db.Abstractions;
using Identity.Business.Users.Models;

namespace Identity.Business.Users.Db
{
    public class PasswordRecoveryRepository : BaseRepositoryWithAccount<PasswordRecovery>, IPasswordRecoveryRepository
    {
        public PasswordRecoveryRepository(
            IBaseRepository<PasswordRecovery> baseRepository,
            IHelpersWithAccount<PasswordRecovery> helpers) : base(baseRepository, helpers)
        {
        }
    }
}