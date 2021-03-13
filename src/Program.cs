using AlbedoTeam.Sdk.JobWorker;

namespace Identity.Business.Users
{
    internal static class Program
    {
        private static void Main()
        {
            Worker.Configure<Startup>().Run();
        }
    }
}