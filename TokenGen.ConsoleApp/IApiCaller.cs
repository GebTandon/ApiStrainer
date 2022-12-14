using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TokenGenLib;

namespace TokenGen.ConsoleApp
{
  // GTan: Example of how to hand wire the calls to tokenrepo during embedded mode.
  public interface IApiCaller
  {
    public Task DoWhatEverWithApiAsync();
  }

  public class ApiCallerService1 : IApiCaller
  {
    private const string MyServerName = "Pretend Server 1"; //must match one of ApiLimitSetting's 'ApiServer' property from Config file.
    private const string MyClientName = nameof(ApiCallerService1);

    private const int nmberOfCalls = 10;
    private const int simulatedApiDurationInMillSecs = 10;

    private ILogger<ApiCallerService1> _logger;
    private IGrantToken _tokenService;

    public ApiCallerService1(ILogger<ApiCallerService1> logger, Func<string, IGrantToken> tokenGrantor)
    {
      _logger = logger;
      _tokenService = tokenGrantor(MyServerName);
    }

    public async Task DoWhatEverWithApiAsync()
    {
      var result = await CallExternalApiInParallelAsync();

      await Task.Delay(TimeSpan.FromSeconds(10));

      await ReleaseTokensAsync(result);

      Console.WriteLine("Press return to exit program...");
      Console.ReadLine();
    }

    private async Task<Token[]> CallExternalApiInParallelAsync()
    {
      var listOfTokenTasks = new List<Task<Token>>();
      for (var i = 0; i < nmberOfCalls; i++)
      {
        listOfTokenTasks.Add(Task.Run(async () =>
        {
          Token token = _tokenService.Obtain(MyClientName);
          Console.WriteLine($"Received Token {token.Id} for client {MyClientName}");
          await Task.Delay(simulatedApiDurationInMillSecs).ConfigureAwait(false);//emulate calling a WebApi.
          return token;
        }));
      }
      var result = await Task.WhenAll(listOfTokenTasks).ConfigureAwait(false);
      return result;
    }

    private async Task ReleaseTokensAsync(IEnumerable<Token> result)
    {
      var listReleaseTasks = new List<Task>();
      foreach (var item in result)
      {
        listReleaseTasks.Add(Task.Run(() => _tokenService.Release(MyClientName, item.Id)));
      }
      await Task.WhenAll(listReleaseTasks).ConfigureAwait(false);
      Console.WriteLine($"Released tokens...");
    }

  }

  public class ApiCallerService2 : IApiCaller
  {
    private const string MyServerName = "Pretend Server 2"; //must match one of ApiLimitSetting's 'ApiServer' property from Config file.
    private const string MyClientName = nameof(ApiCallerService2);

    private const int nmberOfCalls = 10;
    private const int simulatedApiDurationInMillSecs = 10;

    private ILogger<ApiCallerService1> _logger;
    private IGrantToken _tokenService;

    public ApiCallerService2(ILogger<ApiCallerService1> logger, Func<string, IGrantToken> tokenGrantor)
    {
      _logger = logger;
      _tokenService = tokenGrantor(MyServerName);
    }

    public async Task DoWhatEverWithApiAsync()
    {
      var result = await CallExternalApiInParallelAsync();

      await Task.Delay(TimeSpan.FromSeconds(10));

      await ReleaseTokensAsync(result);

      Console.WriteLine("Press return to exit program...");
      Console.ReadLine();
    }

    private async Task<Token[]> CallExternalApiInParallelAsync()
    {
      var listOfTokenTasks = new List<Task<Token>>();
      for (var i = 0; i < nmberOfCalls; i++)
      {
        listOfTokenTasks.Add(Task.Run(async () =>
        {
          Token token = _tokenService.Obtain(MyClientName);
          Console.WriteLine($"Received Token {token.Id} for client {MyClientName}");
          await Task.Delay(simulatedApiDurationInMillSecs).ConfigureAwait(false);//emulate calling a WebApi.
          return token;
        }));
      }
      var result = await Task.WhenAll(listOfTokenTasks).ConfigureAwait(false);
      return result;
    }

    private async Task ReleaseTokensAsync(IEnumerable<Token> result)
    {
      var listReleaseTasks = new List<Task>();
      foreach (var item in result)
      {
        listReleaseTasks.Add(Task.Run(() => _tokenService.Release(MyClientName, item.Id)));
      }
      await Task.WhenAll(listReleaseTasks).ConfigureAwait(false);
      Console.WriteLine($"Released tokens...");
    }

  }
}
