using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TokenGen.ConsoleApp.Settings;
using TokenGenLib;

namespace TokenGen.ConsoleApp
{
  public class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
      var hostBuilder = new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseEnvironment("Development")
                .ConfigureHostConfiguration(cfgBldr =>
                {
                  cfgBldr.SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("hostsettings.json", optional: true)
                  .AddEnvironmentVariables(prefix: "PREFIX_")
                  .AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                  config.SetBasePath(Directory.GetCurrentDirectory());
                  var env = hostingContext.HostingEnvironment.EnvironmentName;
                  config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                  config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
                  config.AddEnvironmentVariables("CSL_");
                  config.AddCommandLine(args);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                  logging.ClearProviders();
                  logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                  logging.AddDebug(); //On Linux, this writes to /var/log/message.
                  //logging.AddEventSourceLogger();
                  //logging.AddEventLog();  //works only on windows deployments.
                  //                        //Add filters for logging through code, (besides in the config file).
                  logging.AddConsole(options => options.IncludeScopes = true);// enable scope in code.
                  logging.AddFilter("System", LogLevel.Debug)
                    .AddFilter<ConsoleLoggerProvider>("Microsoft", LogLevel.Trace)
                    .AddFilter((provider, category, logLevel) =>
                    { //an example of filter function..
                      return provider != "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider" ||
                          category != "TodoApiSample.Controllers.TodoController";
                    });
                  logging.SetMinimumLevel(LogLevel.Trace);
                })
                .UseDefaultServiceProvider(serviceProviderOptions =>
                {
                  serviceProviderOptions.ValidateScopes = true;
                  serviceProviderOptions.ValidateOnBuild = true;
                })
                .ConfigureServices((hostContext, services) =>
                {
                  var configuration = hostContext.Configuration;
                  services.Configure<ApiLimitSetting>((icfg) =>
                  {//default settings.
                    icfg.ApiServer = "Api Server";
                    icfg.MaxRateLimit = 1;
                    icfg.Blocking = false;
                    icfg.RestDuration = TimeSpan.FromSeconds(-1);
                    icfg.WatchDuration = TimeSpan.FromSeconds(-1);
                    icfg.MaxForDuration = 0;
                  });
                  var tokenMonitor = new RegisterTokenMonitor(services);
                  ConfigureTokenMonitorFromConfigName(services, configuration, tokenMonitor, "ApiLimitSetting");
                  //ConfigureTokenMonitorFromConfigName(services, tokenMonitor, "XXXSettings"); //Yatin: Register more Api Servers as needed, encouraged only if using the TokenGenLib as Inprocess monitor.
                })
                ;
      return hostBuilder;
    }

    private static void ConfigureTokenMonitorFromConfigName(IServiceCollection services, IConfiguration configuration, RegisterTokenMonitor tokenMonitor, string appSettingsSectionName)
    {
      var sec = configuration.GetSection(appSettingsSectionName);
      services.Configure<ApiLimitSetting>(sec);
      var limitSet = sec.Get<ApiLimitSetting>();
      tokenMonitor.Register(limitSet.ApiServer, new ApiLimits
      {
        Blocking = limitSet.Blocking,
        MaxForDuration = limitSet.MaxForDuration,
        MaxRateLimit = limitSet.MaxRateLimit,
        RestDuration = limitSet.RestDuration,
        WatchDuration = limitSet.WatchDuration
      });
      Console.WriteLine($"Registered Token Server to Monitor {limitSet.ApiServer}");
    }

  }
}
