using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace TokenGenLib.Internal
{

  public class BlockingTokenRepo : ITokenRepository, ILimitRate, IDisposable
  {
    BlockingCollection<TokenInt> _tokenCache;
    private IConfigureApiLimits _configureThrottle;
    public event EventHandler<TokenIssuedEventArgs> TokenIssued;
    public event EventHandler<MaxTokenIssuedEventArgs> MaxTokenIssued;
    const string constTokenId = "ThrowAwayToken";

    public BlockingTokenRepo(IConfigureApiLimits configureThrottle)
    {
      _configureThrottle = configureThrottle;
      _tokenCache = new BlockingCollection<TokenInt>(_configureThrottle.RateLimit);//Yatin: possible error here , as the Remove operation can be done on multiple threads !!
    }

    public string Name => _configureThrottle.Server;

    public TokenInt PullToken(string client)
    {
      if (string.IsNullOrEmpty(client)) throw new ArgumentNullException(client);
      TokenInt token = GenerateToken((this as ITokenRepository).Name, client);
      _tokenCache.Add(token);//This should block in case we exceed planned tokens.
      OnTokenIssued(token);
      return token;
    }

    public void ReturnToken(string tokenId)
    {
      if (string.IsNullOrEmpty(tokenId) || !string.Equals(tokenId, constTokenId, StringComparison.InvariantCultureIgnoreCase)) return;// throw new ArgumentNullException($"Token Id cannot be null or empty");
      var extractedToken = _tokenCache.Take();
    }

    private TokenInt GenerateToken(string server, string client)
    {
      return new TokenInt { Id = constTokenId, IssuedOn = DateTime.Now, Client = client, Server = server };
    }


    #region Events
    void OnTokenIssued(TokenInt token)
    {
      AsyncEventsHelper.RaiseEventAsync(TokenIssued, this, new TokenIssuedEventArgs { Token = token.Id, Client = token.Client, Time = token.IssuedOn });
    }

    //cannot raise this event since this is blocking token repo.
    void OnMaxTokenIssued(TokenInt token, int counter)
    {
      AsyncEventsHelper.RaiseEventAsync(MaxTokenIssued, this, new MaxTokenIssuedEventArgs { Counts = counter, Client = token.Client, Time = token.IssuedOn });
    }
    #endregion Events

    private string GetTokenId()
    {
      return nameof(BlockingTokenRepo);//same constant string, since blocking collection is just a bag of items, no specific item can be returned.
      //return Guid.NewGuid().ToString();
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          _tokenCache.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~BlockingTokenCollection()
    // {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
