using System;
using Microsoft.Extensions.DependencyInjection;
using TokenGenLib.Internal;
using TokenGenLib.TokenRepos;

namespace TokenGenLib.Services
{
  public static class ServiceCollectionExtension
  {
    public static void AddRateLimitTokenRepository(this IServiceCollection services, string server, int maxRateLimit, bool blocking = false)
    {
      AddTokenRepository(services, server, maxRateLimit, TimeSpan.Zero, TimeSpan.Zero, int.MinValue, blocking);
    }
    public static void AddWindowLimitTokenRepository(this IServiceCollection services, string server, TimeSpan restDuration, TimeSpan watchDuration, int maxForDuration = int.MinValue)
    {
      AddTokenRepository(services, server, int.MinValue, restDuration, watchDuration, maxForDuration, false);
    }
    public static void AddTokenRepository(this IServiceCollection services, string server, int maxRateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxForDuration = int.MinValue, bool blocking = false)
    {

      //An entity should not be a service that is injected.. it should simply be instantiated and used....
      //services.AddSingleton<IConfigureApiLimits>((services) =>
      //{
      //  var retVal = new ApiLimitsConfig() as IConfigureApiLimits;
      //  retVal.Setup(server, maxRateLimit, restDuration, watchDuration, maxForDuration, blocking);
      //  return retVal;
      //});
      var apiLimitsConfig = new ApiLimitsConfig();
      apiLimitsConfig.Setup(server, maxRateLimit, restDuration, watchDuration, maxForDuration, blocking);

      if (maxRateLimit > 0)
        services.AddSingleton<ITokenRepository>((services) =>
        {
          var retVal = blocking
              ? new BlockingTokenRepo(apiLimitsConfig) as ITokenRepository
              : new NonBlockingTokenRepo(apiLimitsConfig) as ITokenRepository;
          return retVal;
        });
      if (maxForDuration > 0)
        services.AddSingleton<ITokenRepository>((services) =>
        {
          var retVal = new FixWindowTokenRepo(apiLimitsConfig) as ITokenRepository;
          return retVal;
        });
      if (maxForDuration > 0 && maxRateLimit > 0) //injects only 2 ITokenRepository
        services.AddTransient<IGrantToken, MultiTokenApiGateway>();
      else if (maxForDuration > 0 || maxRateLimit > 0) //injects only 1 ITokenRepository
        services.AddTransient<IGrantToken, SingleTokenApiGateway>();
      else
        throw new InvalidOperationException("ITokenRepository cannot be determined, make sure configuration is valid to inject at least 1 ITokenRepository.");
    }
  }
}