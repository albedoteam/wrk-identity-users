namespace Identity.Business.Users.Mappers
{
    using System.Collections.Generic;
    using Abstractions;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using AutoMapper;
    using Models;

    public class UserMapper : IUserMapper
    {
        private readonly IMapper _mapper;

        public UserMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                // request to model
                cfg.CreateMap<CreateUser, User>().ReverseMap();

                // model to response
                cfg.CreateMap<User, UserResponse>(MemberList.Destination)
                    .ForMember(t => t.Id, opt => opt.MapFrom(o => o.Id.ToString()));

                // model to event
            });

            _mapper = config.CreateMapper();
        }

        public User RequestToModel(CreateUser request)
        {
            return _mapper.Map<CreateUser, User>(request);
        }

        public UserResponse MapModelToResponse(User model)
        {
            return _mapper.Map<User, UserResponse>(model);
        }

        public List<UserResponse> MapModelToResponse(List<User> model)
        {
            return _mapper.Map<List<User>, List<UserResponse>>(model);
        }
    }
}