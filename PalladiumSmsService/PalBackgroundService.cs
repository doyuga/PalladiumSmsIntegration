namespace PalladiumSmsService
{
    public sealed class PalBackgroundService : BackgroundService
    {
        private readonly ILogger<PalBackgroundService> _logger;
        private readonly SmsService _smsService;

        public PalBackgroundService(
            SmsService smsService,
            ILogger<PalBackgroundService> logger) =>
            (_smsService, _logger) = (smsService, logger);
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await  _smsService.sendinvoicenotification();
                    await _smsService.sendpaymentnotification();
                   // _logger.LogWarning("{Joke}", joke);

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }
    }
}