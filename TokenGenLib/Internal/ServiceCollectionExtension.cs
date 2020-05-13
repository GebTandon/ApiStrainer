using System;
using Microsoft.Extensions.DependencyInjection;
using TokenGenLib.Internal;
using TokenGenLib.TokenRepos;

namespace TokenGenLib.Services
{
  public static class ServiceCollectionExtension
  {
    public static void AddRateLimitTokenDependency(this IServiceCollection services, string server, int maxRateLimit, bool blocking = false)
    {
      AddTokenDependencies(services, server, maxRateLimit, TimeSpan.Zero, TimeSpan.Zero, int.MinValue, blocking);
    }
    public static void AddWindowLimitTokenDependency(this IServiceCollection services, string server, TimeSpan restDuration, TimeSpan watchDuration, int maxForDuration = int.MinValue)
    {
      AddTokenDependencies(services, server, int.MinValue, restDuration, watchDuration, maxForDuration, false);
    }
    public static void AddTokenDependencies(this IServiceCollection services, string server, int maxRateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxForDuration = int.MinValue, bool blocking = false)
    {
      services.AddSingleton((services) =>
      {
        var retVal = new ApiLimitsConfig() as IConfigureApiLimits;
        retVal.Setup(server, maxRateLimit, restDuration, watchDuration, maxForDuration, false);
        return retVal;
      });
      //services.AddSingleton<IKeepStats>((services) =>
      //{
      //  var tokenServices = services.GetServices<ITokenRepository>() as IList<ITokenRepository>;
      //  return new StatsKeeper(tokenServices);
      //});
      if (maxRateLimit > 0)
        services.AddSingleton((services) =>
        {
          var configService = services.GetRequiredService<IConfigureApiLimits>();
          var retVal = blocking
              ? new BlockingTokenRepo(configService) as ITokenRepository
              : new NonBlockingTokenRepo(configService) as ITokenRepository;
          return retVal;
        });
      if (maxForDuration > 0)
        services.AddSingleton((services) =>
        {
          var configService = services.GetRequiredService<IConfigureApiLimits>();
          var retVal = new FixWindowTokenRepo(configService);
          return retVal;
        });
    }
  }
}