using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenGenLib.Internal;
using TokenGenLib.TokenRepos;

namespace TokenGenLib.Services
{
  public static class ServiceCollectionExtension
  {
    public static void AddRateLimitTokenRepository(this IServiceCollection services, string server, int maxRateLimit, bool blocking = false)
    {
      AddTokenRepositoryV2(services, server, maxRateLimit, TimeSpan.Zero, TimeSpan.Zero, int.MinValue, blocking);
    }
    public static void AddWindowLimitTokenRepository(this IServiceCollection services, string server, TimeSpan restDuration, TimeSpan watchDuration, int maxForDuration = int.MinValue)
    {
      AddTokenRepositoryV2(services, server, int.MinValue, restDuration, watchDuration, maxForDuration, false);
    }

    [Obsolete("ITokenRepository is not a service exposed for general public, hence its encapsulated by IGrantToken implementations.", true)]
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
          var logFactory = services.GetRequiredService<ILoggerFactory>();
          var retVal = blocking
              ? new BlockingTokenRepo(apiLimitsConfig, logFactory.CreateLogger<BlockingTokenRepo>()) as ITokenRepository
              : new NonBlockingTokenRepo(apiLimitsConfig, logFactory.CreateLogger<NonBlockingTokenRepo>()) as ITokenRepository;
          return retVal;
        });
      if (maxForDuration > 0)
        services.AddSingleton<ITokenRepository>((services) =>
        {
          var logFactory = services.GetRequiredService<ILoggerFactory>();
          var retVal = new FixWindowTokenRepo(apiLimitsConfig, logFactory.CreateLogger<FixWindowTokenRepo>()) as ITokenRepository;
          return retVal;
        });
      if (maxForDuration > 0 && maxRateLimit > 0) //injects only 2 ITokenRepository
        services.AddTransient<IGrantToken, MultiTokenApiGateway>();
      else if (maxForDuration > 0 || maxRateLimit > 0) //injects only 1 ITokenRepository
        services.AddTransient<IGrantToken, SingleTokenApiGateway>();
      else
        throw new InvalidOperationException("ITokenRepository cannot be determined, make sure configuration is valid to inject at least 1 ITokenRepository.");
    }


    public static void AddTokenRepositoryV2(this IServiceCollection services, string server, int maxRateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxForDuration = int.MinValue, bool blocking = false)
    {
      var apiLimitsConfig = new ApiLimitsConfig();
      apiLimitsConfig.Setup(server, maxRateLimit, restDuration, watchDuration, maxForDuration, blocking);
      var sp = services.BuildServiceProvider();
      var logFactory = sp.GetRequiredService<ILoggerFactory>();

      if (maxForDuration > 0 && maxRateLimit > 0) //injects only 2 ITokenRepository
        services.AddSingleton<IGrantToken>(sevs =>
        {
          return new MultiTokenApiGateway(apiLimitsConfig, logFactory);
        });
      else if (maxForDuration > 0 || maxRateLimit > 0) //injects only 1 ITokenRepository
        services.AddSingleton<IGrantToken>(sevs =>
        {
          return new SingleTokenApiGateway(apiLimitsConfig, logFactory);
        });
      else
        throw new InvalidOperationException("ITokenRepository cannot be determined, make sure configuration is valid to inject at least 1 ITokenRepository.");
    }
  }
}