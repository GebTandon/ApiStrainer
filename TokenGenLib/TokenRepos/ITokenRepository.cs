using System;

namespace TokenGenLib.Internal
{
  public interface ITokenRepository
  {
    public string Name { get; }
    public TokenInt PullToken(string client);
    public void ReturnToken(string tokenId);
    public event EventHandler<TokenIssuedEventArgs> TokenIssued;
    public event EventHandler<MaxTokenIssuedEventArgs> MaxTokenIssued;
  }


  public interface ILimitRate { }
  public interface ILimitWindow { }


  public class BaseTokenRepository
  {
    protected IConfigureApiLimits _configureThrottle;
    public event EventHandler<TokenIssuedEventArgs> TokenIssued;
    public event EventHandler<MaxTokenIssuedEventArgs> MaxTokenIssued;

    public BaseTokenRepository(IConfigureApiLimits configureThrottle)
    {
      _configureThrottle = configureThrottle;
    }
    
    public string Name => _configureThrottle.Server;

    #region Events
    protected virtual void OnTokenIssued(TokenInt token)
    {
      AsyncEventsHelper.RaiseEventAsync(TokenIssued, this, new TokenIssuedEventArgs { Token = token.Id, Client = token.Client, Time = token.IssuedOn });
    }

    //cannot raise this event since this is blocking token repo.
    protected virtual void OnMaxTokenIssued(string client, int counter)
    {
      AsyncEventsHelper.RaiseEventAsync(MaxTokenIssued, this, new MaxTokenIssuedEventArgs { Counts = counter, Client = client, Time = DateTime.Now });
    }
    #endregion Events

  }

}
