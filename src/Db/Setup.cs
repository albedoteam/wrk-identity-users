using Identity.Business.Users.Db.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Business.Users.Db
{
    public static class Setup
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}