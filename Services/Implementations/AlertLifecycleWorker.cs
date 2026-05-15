using DaNangSafeMap.Services.Interfaces;

namespace DaNangSafeMap.Services.Implementations
{
    public class AlertLifecycleWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertLifecycleWorker> _logger;

        public AlertLifecycleWorker(IServiceScopeFactory scopeFactory, ILogger<AlertLifecycleWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                    await alertService.ProcessExpiredAlertsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể chạy worker xử lý SLA và vòng đời báo cáo");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
