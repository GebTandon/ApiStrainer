using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TokenGenLib.Internal
{
  /// <summary>
  /// // GTan:
  /// The benefit of using this approach over just a SemphoreSlim is that the code cannot be foolled by over-retruning tokens.
  /// We can validate that the tokens are valid and were not returned earlier in following approach.
  /// </summary>
  public class NonBlockingTokenRepo : BaseTokenRepository, ITokenRepository, ILimitRate
  {
    private readonly ILogger<NonBlockingTokenRepo> _logger;
    readonly ConcurrentDictionary<string, TokenInt> _tokenCache;

    public NonBlockingTokenRepo(IConfigureApiLimits configureThrottle, ILogger<NonBlockingTokenRepo> logger) : base(configureThrottle)
    {
      _logger = logger;
      _tokenCache = new ConcurrentDictionary<string, TokenInt>(1, configureThrottle.RateLimit);// GTan: possible error here , as the Remove operation can be done on multiple threads !! Might have to change 1 to a number = maxLimit
    }

    public TokenInt PullToken(string client)
    {
      if (string.IsNullOrEmpty(client)) throw new ArgumentNullException(client);

      var token = GenerateToken((this as ITokenRepository).Name, client);
      lock (_tokenCache)
      {
        if (_tokenCache.Count < _configureThrottle.RateLimit && _tokenCache.TryAdd(token.Id, token))
        {
          OnTokenIssued(token);
          return token;
        }
        _logger?.LogWarning($"Hit Token Rate Limits for Server: {_configureThrottle.Server} Limit:{_configureThrottle.RateLimit}");
        OnMaxTokenIssued(client, _configureThrottle.RateLimit);
        return null;
      }
    }

    public void ReturnToken(string tokenId)
    {
      if (string.IsNullOrEmpty(tokenId)) return;// throw new ArgumentNullException($"Token Id cannot be null or empty");
      if (!Guid.TryParse(tokenId, out Guid noUse)) return;

      if (!_tokenCache.TryRemove(tokenId, out TokenInt existingValue))
      {
        //_logger?.LogWarning($"The token {tokenId} for server {_configureThrottle.Server} was already removed !!");
      }
    }

    private TokenInt GenerateToken(string server, string client)
    {
      return new TokenInt { Id = Guid.NewGuid().ToString(), IssuedOn = DateTime.Now, Client = client, Server = server };
    }

  }
}
