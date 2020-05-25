using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace TokenGenLib.Fody
{
  //https://www.davidezoccarato.cloud/resolving-instances-with-asp-net-core-di-in-static-classes/
  public class ServiceActivator
  {
    internal static IServiceProvider _serviceProvider = null;

    public static void Configure(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    public static IServiceScope GetScope(IServiceProvider serviceProvider = null)
    {
      var provider = serviceProvider ?? _serviceProvider;
      return provider?
          .GetRequiredService<IServiceScopeFactory>()
          .CreateScope();
    }
  }
}
