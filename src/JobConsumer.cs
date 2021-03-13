using System.Threading;
using System.Threading.Tasks;
using AlbedoTeam.Sdk.JobWorker.Configuration.Abstractions;
using AlbedoTeam.Sdk.MessageConsumer.Configuration.Abstractions;
using Microsoft.Extensions.Logging;

namespace Identity.Business.Users
{
    public class JobConsumer : IJobRunner
    {
        private readonly IBusRunner _busRunner;
        private readonly ILogger<JobConsumer> _logger;

        public JobConsumer(ILogger<JobConsumer> logger, IBusRunner busRunner)
        {
            _logger = logger;
            _busRunner = busRunner;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("JobConsumer is starting...");
            await _busRunner.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("...JobConsumer is stopped");
            await _busRunner.StopAsync(cancellationToken);
        }

        public async Task TickAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("JobConsumer is running: {Bus}", _busRunner.Who);
            await Task.Delay(3000, cancellationToken);
        }
    }
}