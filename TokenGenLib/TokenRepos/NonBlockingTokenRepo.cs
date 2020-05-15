using System;
using System.Collections.Concurrent;

namespace TokenGenLib.Internal
{
  public class NonBlockingTokenRepo : BaseTokenRepository, ITokenRepository, ILimitRate
  {
    readonly ConcurrentDictionary<string, TokenInt> _tokenCache;

    public NonBlockingTokenRepo(IConfigureApiLimits configureThrottle) : base(configureThrottle)
    {
      _tokenCache = new ConcurrentDictionary<string, TokenInt>(1, configureThrottle.RateLimit);// Yatin: possible error here , as the Remove operation can be done on multiple threads !! Might have to change 1 to a number = maxLimit
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
        //_logger?.LogWarning($"Looks like the token {token.Id} for client {token.Client} was already removed !!");
      }
    }

    private TokenInt GenerateToken(string server, string client)
    {
      return new TokenInt { Id = Guid.NewGuid().ToString(), IssuedOn = DateTime.Now, Client = client, Server = server };
    }

  }
}
