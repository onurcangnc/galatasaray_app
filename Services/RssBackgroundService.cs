using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RssNewsApp.Services
{
    public class RssBackgroundService : BackgroundService
    {
        private readonly ILogger<RssBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly PeriodicTimer _timer;

        public RssBackgroundService(
            ILogger<RssBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(10)); // 10 saniyede bir güncelle
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var rssService = scope.ServiceProvider.GetRequiredService<IRssService>();
                    await rssService.UpdateNewsAsync();
                    _logger.LogInformation("RSS haberleri güncellendi");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RSS güncelleme hatası");
                }
            }
        }
    }
} 