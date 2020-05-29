using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenGenLib;
using TokenGenLib.Fody;

namespace TokenGen.Ext
{
  //This is the class that will be entry point to call in from a Fody weaver....
  public static class TokenGenExt
  {
    public static Token GenerateToken(string uri)
    {
      ILogger _logger = null;
      Token _token = null;
      var clientName = "";
      var serverName = "";
      try
      {
        using var serviceScope = ServiceActivator.GetScope();
        clientName = ProcessExtractor.ProcessName();
        serverName = ServerExtractor.ServerName(new Uri(uri));
        _logger = serviceScope?.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger("TokenGenExt");
        var igrantToken = serviceScope?.ServiceProvider?.GetServices<IGrantToken>()?.FirstOrDefault(x => x.ServerName.Equals(serverName, StringComparison.InvariantCultureIgnoreCase));
        _token = igrantToken?.Obtain(clientName);
      }
      catch (Exception e)
      {
        _logger?.LogError(e, $"--Exception while fetching token for Server: {serverName} & Client:{clientName}");
      }
      return _token;
    }

    public static void ReleaseToken(Token token, string uri)
    {
      ILogger _logger = null;
      var clientName = "";
      var serverName = "";
      try
      {
        using var serviceScope = ServiceActivator.GetScope();
        clientName = ProcessExtractor.ProcessName();
        serverName = ServerExtractor.ServerName(new Uri(uri));
        _logger = serviceScope?.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger("TokenGenExt");
        var igrantToken = serviceScope?.ServiceProvider?.GetServices<IGrantToken>()?.FirstOrDefault(x => x.ServerName.Equals(serverName, StringComparison.InvariantCultureIgnoreCase));
        igrantToken?.Release(clientName, token?.Id);
      }
      catch (Exception e)
      {
        _logger?.LogError(e, $"--Exception while releasing token for Server: {serverName} & Client:{clientName}");
      }
    }
  }
}
