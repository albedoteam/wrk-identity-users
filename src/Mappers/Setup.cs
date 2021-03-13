using Identity.Business.Users.Mappers.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Business.Users.Mappers
{
    public static class Setup
    {
        public static IServiceCollection AddMappers(this IServiceCollection services)
        {
            services.AddTransient<IUserMapper, UserMapper>();

            return services;
        }
    }
}