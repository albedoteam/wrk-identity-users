namespace Identity.Business.Users.Mappers
{
    using System.Collections.Generic;
    using Abstractions;
    using AlbedoTeam.Identity.Contracts.Requests;
    using AlbedoTeam.Identity.Contracts.Responses;
    using AlbedoTeam.Sdk.DataLayerAccess.Utils.Query;
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

                // request -> query
                cfg.CreateMap<ListUsers, QueryParams>(MemberList.Destination)
                    .ForMember(l => l.Sorting, opt => opt.MapFrom(o => o.Sorting.ToString()));
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

        public QueryParams RequestToQuery(ListUsers request)
        {
            return _mapper.Map<ListUsers, QueryParams>(request);
        }
    }
}