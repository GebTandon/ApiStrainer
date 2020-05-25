using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TokenGenLib;

namespace TokenGen.ConsoleApp
{
  public interface IUseFody
  {
    public Task CallAWebApiAsync();
    public void CallAWebApi();
  }

  public class FodyAttributeDemo : IUseFody
  {
    private ILogger<FodyAttributeDemo> _logger;

    public FodyAttributeDemo(ILogger<FodyAttributeDemo> logger)
    {
      _logger = logger;
    }

    [EmbeddedTokenRepo(ClientName = "EmbeddedApiClient", ServerName = "EmbeddedServer")]
    public void CallAWebApi()
    {
      _logger.LogInformation($"Executing method {nameof(FodyAttributeDemo)}.{nameof(CallAWebApi)}");
    }

    [EmbeddedTokenRepo(ClientName = "EmbeddedApiClient", ServerName = "EmbeddedServer")]
    public async Task CallAWebApiAsync()
    {
      await Task.CompletedTask;
    }
  }
}
