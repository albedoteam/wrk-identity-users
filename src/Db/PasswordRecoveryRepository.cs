namespace Identity.Business.Users.Db
{
    using Abstractions;
    using AlbedoTeam.Sdk.DataLayerAccess;
    using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
    using Models;

    public class PasswordRecoveryRepository : BaseRepositoryWithAccount<PasswordRecovery>, IPasswordRecoveryRepository
    {
        public PasswordRecoveryRepository(
            IBaseRepository<PasswordRecovery> baseRepository,
            IHelpersWithAccount<PasswordRecovery> helpers) : base(baseRepository, helpers)
        {
        }
    }
}