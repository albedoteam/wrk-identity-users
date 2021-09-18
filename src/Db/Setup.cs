namespace Identity.Business.Users.Db
{
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;

    public static class Setup
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPasswordRecoveryRepository, PasswordRecoveryRepository>();

            return services;
        }
    }
}