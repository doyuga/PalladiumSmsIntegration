using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using PalladiumSmsService;
using System.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx,services) =>
    {
        IConfiguration configuration = ctx.Configuration;
        LoggerProviderOptions.RegisterProviderOptions<
            EventLogSettings, EventLogLoggerProvider>(services);
        services.AddSingleton<SmsService>();
        services.AddHostedService<PalSmsService>();
        services.Configure<ConnectionStrings>(configuration.GetSection(nameof(ConnectionStrings))); ;
    })
    .ConfigureLogging((context, logging) =>
    {

        logging.AddConfiguration(
            context.Configuration.GetSection("Logging"));
    })
    .ConfigureLogging(logBuilder =>
     {
           logBuilder.SetMinimumLevel(LogLevel.Trace);
           logBuilder.AddLog4Net("app.config");

      })
     //.ConfigureLogging(loggerFactory => loggerFactory.AddEventLog())

     .UseWindowsService(options =>
        {
            options.ServiceName = "Palladium Sms Service";
        })
    .Build();

host.Run();
