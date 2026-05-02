using Microsoft.Extensions.Hosting;

namespace Infrastructure.BackgroundJob
{
    public class QueuedHostService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;

        public QueuedHostService(IBackgroundTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem;
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
        }
    }
}
