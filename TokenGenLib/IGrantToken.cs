using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TokenGenLib.Internal;
using TokenGenLib.TokenRepos;

namespace TokenGenLib
{

  public interface IGrantToken
  {
    public string ServerName { get; }
    public Token Obtain(string client);
    public void Release(string client, string tokenId);
  }

  public class MultiTokenApiGateway : IGrantToken
  {
    private readonly ITokenRepository _iLimitRate;//Only tokens obtained from RateLimit needs to be returned.
    private readonly ITokenRepository _iLimitWindow;//Token needs to be issued, but not returned..
    private ApiLimitsConfig _apiLimitsConfig;

    public string ServerName => _apiLimitsConfig.Server;

    [Obsolete("No Di needed, since it encapsulates the tokenRepo creation, it not a shared service...", true)]
    public MultiTokenApiGateway(IList<ITokenRepository> tokenRepos)
    {
      if (tokenRepos.Count < 2) return; //Something is wrong.. we should have got 2 ITokenRepository...

      if (tokenRepos[0] is ILimitRate)
        _iLimitRate = tokenRepos[0];
      else
        _iLimitWindow = tokenRepos[0];
      if (tokenRepos[1] is ILimitWindow)
        _iLimitWindow = tokenRepos[1];
      else
        _iLimitRate = tokenRepos[1];
    }
    public MultiTokenApiGateway(ApiLimitsConfig apiLimitsConfig, ILoggerFactory loggerFactory)
    {
      _apiLimitsConfig = apiLimitsConfig;
      _iLimitRate = apiLimitsConfig.IsBlocking
        ? new BlockingTokenRepo(apiLimitsConfig, loggerFactory.CreateLogger<BlockingTokenRepo>()) as ITokenRepository
        : new NonBlockingTokenRepo(apiLimitsConfig, loggerFactory.CreateLogger<NonBlockingTokenRepo>()) as ITokenRepository;
      _iLimitWindow = new FixWindowTokenRepo(apiLimitsConfig, loggerFactory.CreateLogger<FixWindowTokenRepo>()) as ITokenRepository;
    }

    public Token Obtain(string client)
    {
      var rateLimitToken = (Token)_iLimitRate.PullToken(client);
      _iLimitWindow.PullToken(client); // GTan: this token is not required to be returned.
      return rateLimitToken;
    }

    public void Release(string client, string tokenId)
    {
      _iLimitRate.ReturnToken(tokenId);
    }
  }
  public class SingleTokenApiGateway : IGrantToken
  {
    private readonly ITokenRepository _tokenRepo;
    private ApiLimitsConfig _apiLimitsConfig;
    public string ServerName => _apiLimitsConfig.Server;

    [Obsolete("No Di needed, since it encapsulates the tokenRepo creation, it not a shared service...", true)]
    public SingleTokenApiGateway(ITokenRepository tokenRepo)
    {
      _tokenRepo = tokenRepo;
    }
    public SingleTokenApiGateway(ApiLimitsConfig apiLimitsConfig, ILoggerFactory loggerFactory)
    {
      _apiLimitsConfig = apiLimitsConfig;
      if (apiLimitsConfig.TotalLimit > 0)
        _tokenRepo = new FixWindowTokenRepo(apiLimitsConfig, loggerFactory.CreateLogger<FixWindowTokenRepo>());
      else
        _tokenRepo = apiLimitsConfig.IsBlocking
          ? new BlockingTokenRepo(apiLimitsConfig, loggerFactory.CreateLogger<BlockingTokenRepo>()) as ITokenRepository
          : new NonBlockingTokenRepo(apiLimitsConfig, loggerFactory.CreateLogger<NonBlockingTokenRepo>()) as ITokenRepository;
    }

    public Token Obtain(string client)
    {
      var retVal = (Token)_tokenRepo.PullToken(client);
      return retVal;
    }

    public void Release(string client, string tokenId)
    {
      _tokenRepo.ReturnToken(tokenId);
    }
  }
}
