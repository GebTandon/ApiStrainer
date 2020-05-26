using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TokenGen.ConsoleApp.Plumbing;
using TokenGen.ConsoleApp.Settings;
using TokenGenLib;
using TokenGenLib.Fody;

[module: ApiCallTracker] //Register this Fody Extended Attribute.

namespace TokenGen.ConsoleApp
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var iHost = CreateHostBuilder(args).Build();
      iHost.Run();
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
                  logging.AddConsole(options => options.IncludeScopes = true);// enable scope in code.
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
                  //InProcess multi server tracking..
                  ConfigureTokenMonitorFromConfigName(services, configuration, tokenMonitor, "Server1Limits");
                  ConfigureTokenMonitorFromConfigName(services, configuration, tokenMonitor, "Server2Limits");
                  RegisterTokenMonitorRetrieverFunctionFactory(services);

                  services.AddHostedService<LifetimeEventsHostedService>();//add lifetime listener so we can hook our application process in here...
                  services.AddSingleton<IApiCaller, ApiCallerService1>();
                  services.AddSingleton<IApiCaller, ApiCallerService2>();

                  //Fody testing..
                  services.AddSingleton<IUseFody, FodyAttributeDemo>();
                })
                ;
      return hostBuilder;
    }

    private static void RegisterTokenMonitorRetrieverFunctionFactory(IServiceCollection services)
    {
      services.AddSingleton<Func<string, IGrantToken>>((srvs) =>
      {
        return new Func<string, IGrantToken>(srvrName =>
        {
          var retVal = srvs.GetServices<IGrantToken>().FirstOrDefault(x => x.ServerName.Equals(srvrName, StringComparison.InvariantCultureIgnoreCase));
          return retVal;
        });
      });
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
