using System;
using System.Collections.Concurrent;

namespace TokenGenLib.Internal
{
  public class NonBlockingTokenRepo : ITokenRepository, ILimitRate
  {
    ConcurrentDictionary<string, TokenInt> _tokenCache;
    private IConfigureApiLimits _configureThrottle;
    public event EventHandler<TokenIssuedEventArgs> TokenIssued;
    public event EventHandler<MaxTokenIssuedEventArgs> MaxTokenIssued;

    public NonBlockingTokenRepo(IConfigureApiLimits configureThrottle)
    {
      _configureThrottle = configureThrottle;
      _tokenCache = new ConcurrentDictionary<string, TokenInt>(1, configureThrottle.RateLimit);//Yatin: possible error here , as the Remove operation can be done on multiple threads !! Might have to change 1 to a number = maxLimit
    }

    public string Name => _configureThrottle.Server;

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
        OnMaxTokenIssued(null, _configureThrottle.RateLimit);
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

    #region Events
    void OnTokenIssued(TokenInt token)
    {
      AsyncEventsHelper.RaiseEventAsync(TokenIssued, this, new TokenIssuedEventArgs { Token = token.Id, Client = token.Client, Time = token.IssuedOn });
    }

    void OnMaxTokenIssued(TokenInt token, int counter)
    {
      AsyncEventsHelper.RaiseEventAsync(MaxTokenIssued, this, new MaxTokenIssuedEventArgs { Counts = counter, Client = token.Client, Time = token.IssuedOn });
    }
    #endregion Events

  }
}
