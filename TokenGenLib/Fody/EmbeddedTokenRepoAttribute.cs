using System;
using System.Reflection;
using MethodDecorator.Fody.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenGenLib;
using TokenGenLib.Fody;

[assembly: EmbeddedTokenRepo] //Register this Fody Extended Attribute.

namespace TokenGenLib
{
  //https://weekly-geekly.github.io/articles/263511/index.html
  //https://sudonull.com/post/96364-Improving-Fody-MethodDecoratorEx-for-asynchronous-methods
  [AttributeUsage(AttributeTargets.Method | /*AttributeTargets.Constructor |*/ AttributeTargets.Assembly | AttributeTargets.Module)]
  public class EmbeddedTokenRepoAttribute : Attribute, IMethodDecorator
  {
    private ILogger<EmbeddedTokenRepoAttribute> _logger;
    private object _instance;
    private MethodBase _method;
    private object[] _args;

    public string ServerName { get; set; }
    public string ClientName { get; set; }

    public void Init(object instance, MethodBase method, object[] args)
    {
      //From the instance of the attribute, get the servername, and locate and memorize the API Server token manger
      //using var serviceScope = ServiceActivator.GetScope();
      //_logger = serviceScope.ServiceProvider.GetService<ILoggerFactory>().CreateLogger<EmbeddedTokenRepoAttribute>();
      _instance = instance;
      _method = method;
      _args = args;
    }

    public void OnEntry()
    {
      _logger?.LogInformation($"Entering method:{_method} with Args:{string.Join(",", _args)}");
    }

    public void OnExit()
    {
      _logger?.LogInformation($"Leaving method:{_method} with Args:{string.Join(",", _args)}");
    }

    public void OnException(Exception exception)
    {
      _logger?.LogInformation($"Exception in method:{_method} with Args:{string.Join(",", _args)}");
    }
  }
}
