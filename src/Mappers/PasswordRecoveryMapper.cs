namespace Identity.Business.Users.Mappers
{
    using Abstractions;
    using AlbedoTeam.Identity.Contracts.Responses;
    using AutoMapper;
    using Models;

    public class PasswordRecoveryMapper : IPasswordRecoveryMapper
    {
        private readonly IMapper _mapper;

        public PasswordRecoveryMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                // request to model

                // model to response
                cfg.CreateMap<PasswordRecovery, PasswordRecoveryResponse>(MemberList.Destination)
                    .ForMember(t => t.Id, opt => opt.MapFrom(o => o.Id.ToString()));

                // model to event
            });

            _mapper = config.CreateMapper();
        }


        public PasswordRecoveryResponse MapModelToResponse(PasswordRecovery model)
        {
            return _mapper.Map<PasswordRecovery, PasswordRecoveryResponse>(model);
        }
    }
}