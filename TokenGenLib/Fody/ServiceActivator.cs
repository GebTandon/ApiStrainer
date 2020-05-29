using System;
using Microsoft.Extensions.DependencyInjection;

namespace TokenGenLib.Fody
{
  //https://www.davidezoccarato.cloud/resolving-instances-with-asp-net-core-di-in-static-classes/
  internal static class ServiceActivator
  {
    internal static IServiceProvider _serviceProvider = null;

    internal static void Configure(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    internal static IServiceScope GetScope(IServiceProvider serviceProvider = null)
    {
      var provider = serviceProvider ?? _serviceProvider;
      return provider?
          .GetRequiredService<IServiceScopeFactory>()
          .CreateScope();
    }
  }
}
