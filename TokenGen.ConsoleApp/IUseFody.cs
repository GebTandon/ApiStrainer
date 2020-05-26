using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TokenGenLib.Fody;

namespace TokenGen.ConsoleApp
{
  public interface IUseFody
  {
    public Task CallAWebApiAsync();
    public void CallAWebApi();
  }

  //Web API calling simulation using ApiCallTrackerAttribute....
  public class FodyAttributeDemo : IUseFody
  {
    private ILogger<FodyAttributeDemo> _logger;

    public FodyAttributeDemo(ILogger<FodyAttributeDemo> logger)
    {
      _logger = logger;
    }

    [ApiCallTracker(ClientName = "EmbeddedApiClient", ServerName = "EmbeddedServer")]
    public void CallAWebApi()
    {
      _logger.LogInformation($"Executing method {nameof(FodyAttributeDemo)}.{nameof(CallAWebApi)}");
      Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
    }

    [ApiCallTracker(ClientName = "EmbeddedApiClient", ServerName = "EmbeddedServer")]
    public async Task CallAWebApiAsync()
    {
      _logger.LogInformation($"Executing method {nameof(FodyAttributeDemo)}.{nameof(CallAWebApiAsync)}");
      await Task.Delay(TimeSpan.FromSeconds(10));
    }
  }
}
