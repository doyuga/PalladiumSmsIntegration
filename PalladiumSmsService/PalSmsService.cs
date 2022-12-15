using Microsoft.Extensions.Options;

namespace PalladiumSmsService
{
    public class PalSmsService : BackgroundService
    {
        private readonly ILogger<PalSmsService> _logger;
        private readonly string? _constring;
        private readonly SmsService _smsService;

        public PalSmsService(SmsService smsService, ILogger<PalSmsService> logger, IOptions<ConnectionStrings> options)
                => (_smsService, _logger,_constring) = (smsService, logger,options.Value.notifications);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await _smsService.sendinvoicenotification();
                    await _smsService.sendpaymentnotification();

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                //Environment.Exit(1);
            }
        }
    }
}