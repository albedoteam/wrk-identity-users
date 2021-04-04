namespace Identity.Business.Users.Db.Abstractions
{
    using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
    using Models;

    public interface IUserRepository : IBaseRepositoryWithAccount<User>
    {
    }
}