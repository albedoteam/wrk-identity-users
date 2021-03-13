using System.Collections.Generic;
using AlbedoTeam.Identity.Contracts.Requests;
using AlbedoTeam.Identity.Contracts.Responses;
using Identity.Business.Users.Models;

namespace Identity.Business.Users.Mappers.Abstractions
{
    public interface IUserMapper
    {
        // request to model
        User RequestToModel(CreateUser request);

        // model to response
        UserResponse MapModelToResponse(User model);
        List<UserResponse> MapModelToResponse(List<User> model);
    }
}