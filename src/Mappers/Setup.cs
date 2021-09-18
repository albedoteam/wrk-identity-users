namespace Identity.Business.Users.Mappers
{
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;

    public static class Setup
    {
        public static IServiceCollection AddMappers(this IServiceCollection services)
        {
            services.AddTransient<IUserMapper, UserMapper>();
            services.AddTransient<IPasswordRecoveryMapper, PasswordRecoveryMapper>();

            return services;
        }
    }
}