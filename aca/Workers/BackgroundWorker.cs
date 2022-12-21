
public class BackgroundWorker : BackgroundService
{
    private readonly ILogger<BackgroundWorker> _logger;

    public BackgroundWorker(IBackgroundTaskQueue taskQueue, 
            ILogger<BackgroundWorker> logger)
        {
            TaskQueue = taskQueue;
            _logger = logger;
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service running.");              
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
               
                
                var workItem = 
                    await TaskQueue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Accepted task at: {time}", DateTimeOffset.Now);

                try
                {
                    await workItem(stoppingToken);
                    _logger.LogInformation("Completed task at: {time}", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error occurred executing {WorkItem}.", nameof(workItem));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
}


