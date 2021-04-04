namespace Identity.Business.Users.Mappers.Abstractions
{
    using System.Collections.Generic;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using Models;

    public interface IUserMapper
    {
        // request to model
        User RequestToModel(CreateUser request);

        // model to response
        UserResponse MapModelToResponse(User model);
        List<UserResponse> MapModelToResponse(List<User> model);
    }
}