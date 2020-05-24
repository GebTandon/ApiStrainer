using System;
using Microsoft.Extensions.DependencyInjection;
using TokenGenLib.Services;

namespace TokenGenLib
{
  public interface IRegisterServer
  {
    string RegisteredServer { get; }
    void Register(string apiName, ApiLimits limits);
    void Deregister(string apiName);
  }

  public class RegisterTokenMonitor : IRegisterServer
  {
    private readonly IServiceCollection _services;
    string _registeredServer = "";
    public RegisterTokenMonitor(IServiceCollection services)
    {
      _services = services;
    }

    public string RegisteredServer => _registeredServer;

    public void Deregister(string apiName)
    {
      _registeredServer = string.Empty;
    }

    public void Register(string apiName, ApiLimits limits)
    {
      _registeredServer = apiName;
      _services.AddTokenRepository(apiName, limits.MaxRateLimit, limits.RestDuration, limits.WatchDuration, limits.MaxForDuration, limits.Blocking);
    }
  }

  [Obsolete("Not fully flushed out", true)]
  public interface IStartup
  {
    void Initialize();
    void TearDown();
  }
}
