using System;
using Microsoft.Extensions.DependencyInjection;
using TokenGenLib.Services;

namespace TokenGenLib
{
  public interface IRegisterServer
  {
    void Register(string apiName, ApiLimits limits);
    void Deregister(string apiName);
  }

  public class RegisterTokenMonitor : IRegisterServer
  {
    private readonly IServiceCollection _services;

    public RegisterTokenMonitor(IServiceCollection services)
    {
      _services = services;
    }

    public void Deregister(string apiName)
    {

    }

    public void Register(string apiName, ApiLimits limits)
    {
      _services.AddTokenDependencies(apiName, limits.MaxRateLimit, limits.RestDuration, limits.WatchDuration, limits.MaxForDuration, limits.Blocking);
    }
  }

  [Obsolete("Not fully flushed out", true)]
  public interface IStartup
  {
    void Initialize();
    void TearDown();
  }
}
