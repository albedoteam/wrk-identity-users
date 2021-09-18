namespace Identity.Business.Users.Db.Abstractions
{
    using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
    using Models;

    public interface IPasswordRecoveryRepository : IBaseRepositoryWithAccount<PasswordRecovery>
    {
    }
}