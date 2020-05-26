using System;
using System.Linq;
using System.Reflection;
using MethodDecorator.Fody.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenGenLib.Fody;

[module: ApiCallTracker] //Register this Fody Extended Attribute.

namespace TokenGenLib.Fody
{
  //https://weekly-geekly.github.io/articles/263511/index.html
  //https://sudonull.com/post/96364-Improving-Fody-MethodDecoratorEx-for-asynchronous-methods
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = false, Inherited = true)]
  public class ApiCallTrackerAttribute : Attribute, IMethodDecorator
  {
    private object _hostClassInstance;
    private MethodBase _method;
    private object[] _args;
    private Token _token;

    public string ServerName { get; set; }
    public string ClientName { get; set; }

    public ApiCallTrackerAttribute() : this(null, null) { }
    public ApiCallTrackerAttribute(string serverName, string clientName)
    {
      ServerName = serverName;
      ClientName = clientName;
    }
    public void Init(object hostClassInstance, MethodBase method, object[] args)
    {
      //From the instance of the attribute, get the servername, and locate and memorize the API Server token manger
      _hostClassInstance = hostClassInstance;
      _method = method;
      _args = args;
    }

    public void OnEntry()
    {
      ILogger<ApiCallTrackerAttribute> _logger = null;
      try
      {
        using var serviceScope = ServiceActivator.GetScope();
        _logger = serviceScope?.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ApiCallTrackerAttribute>();
        //_logger?.LogInformation($"Entering method:{_method} with Args:{string.Join(",", _args)}");
        var igrantToken = serviceScope?.ServiceProvider?.GetServices<IGrantToken>()?.FirstOrDefault(x => x.ServerName.Equals(ServerName, StringComparison.InvariantCultureIgnoreCase));
        _token = igrantToken?.Obtain(ClientName);
      }
      catch (Exception e)
      {
        _logger?.LogError(e, $"--Exception while fetching token for Server: {ServerName} & Client:{ClientName}");
      }
    }

    public void OnExit()
    {
      ILogger<ApiCallTrackerAttribute> _logger = null;
      try
      {
        using var serviceScope = ServiceActivator.GetScope();
        _logger = serviceScope?.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ApiCallTrackerAttribute>();
        //_logger?.LogInformation($"Leaving method:{_method} with Args:{string.Join(",", _args)}");
        var igrantToken = serviceScope?.ServiceProvider?.GetServices<IGrantToken>()?.FirstOrDefault(x => x.ServerName.Equals(ServerName, StringComparison.InvariantCultureIgnoreCase));
        igrantToken?.Release(ClientName, _token.Id);
      }
      catch (Exception e)
      {
        _logger?.LogError(e, $"--Exception while releasing token for Server: {ServerName} & Client:{ClientName}");
      }
    }

    public void OnException(Exception exception)
    {
      ILogger<ApiCallTrackerAttribute> _logger = null;
      try
      {
        using var serviceScope = ServiceActivator.GetScope();
        _logger = serviceScope?.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ApiCallTrackerAttribute>();
        //_logger?.LogInformation($"Exception in method:{_method} with Args:{string.Join(",", _args)}");
        var igrantToken = serviceScope?.ServiceProvider?.GetServices<IGrantToken>()?.FirstOrDefault(x => x.ServerName.Equals(ServerName, StringComparison.InvariantCultureIgnoreCase));
        igrantToken?.Release(ClientName, _token.Id);
      }
      catch (Exception e)
      {
        _logger?.LogError(e, $"--Exception while releasing token for Server: {ServerName} & Client:{ClientName}");
      }
    }
  }
}