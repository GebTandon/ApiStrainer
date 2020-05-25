using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TokenGen.ConsoleApp.Plumbing
{
  internal class LifetimeEventsHostedService : IHostedService
  {
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IList<IApiCaller> _apiCallers;
    private IUseFody _useFody;

    public LifetimeEventsHostedService(ILogger<LifetimeEventsHostedService> logger, IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider, IUseFody useFody)
    {
      _logger = logger;
      _appLifetime = appLifetime;
      _apiCallers = new List<IApiCaller>();
      var iapiServices = serviceProvider.GetServices<IApiCaller>(); //Since we are injecting multiple types derived from same interface, this is the way to get instances right.
      var apiCaller1 = iapiServices.First(x => x.GetType() == typeof(ApiCallerService1)) as IApiCaller;
      var apiCaller2 = iapiServices.First(x => x.GetType() == typeof(ApiCallerService2)) as IApiCaller;
      _apiCallers.Add(apiCaller1);
      _apiCallers.Add(apiCaller2);
      _useFody = useFody;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _appLifetime.ApplicationStarted.Register(OnStarted);
      _appLifetime.ApplicationStopping.Register(OnStopping);
      _appLifetime.ApplicationStopped.Register(OnStopped);

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    private void OnStarted()
    {
      _logger.LogInformation("OnStarted has been called.");

      _useFody.CallAWebApiAsync();
      _useFody.CallAWebApi();

      /*    // Yatin: Commented to test Fody
            // Perform post-startup activities here
            Parallel.ForEach(_apiCallers, async (apiCaller) => await apiCaller.DoWhatEverWithApiAsync().ConfigureAwait(false));
      */
    }

    private void OnStopping()
    {
      _logger.LogInformation("OnStopping has been called.");

      // Perform on-stopping activities here
    }

    private void OnStopped()
    {
      _logger.LogInformation("OnStopped has been called.");

      // Perform post-stopped activities here
    }
  }
}
