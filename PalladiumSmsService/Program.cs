using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using PalladiumSmsService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(conf =>
      {
        conf.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
      })

    .UseWindowsService(options =>
    {
        options.ServiceName = "Palladium Sms Service";
    })
        //.ConfigureServices(services =>
        //{
        //    LoggerProviderOptions.RegisterProviderOptions<
        //        EventLogSettings, EventLogLoggerProvider>(services);

        //    services.AddSingleton<SmsService>();
        //    services.AddHostedService<PalBackgroundService>();
            
        //   // services.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
        //})
        //.ConfigureLogging((context, logging) =>
        //{
           
        //    logging.AddConfiguration(
        //        context.Configuration.GetSection("Logging"));
        //})
        .Build();

    await host.RunAsync();
