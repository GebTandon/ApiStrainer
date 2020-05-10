using System;
using System.Timers;
using TokenGenLib.Internal;
using Timer = System.Timers.Timer;

namespace TokenGenLib.TokenRepos
{
  /// <summary>
  /// counts number of calls made within fix window, the window starts when first call is made.
  /// </summary>
  public class FixWindowTokenRepo : ITokenRepository, ILimitWindow, IDisposable
  {
    public event EventHandler<TokenIssuedEventArgs> TokenIssued;
    public event EventHandler<MaxTokenIssuedEventArgs> MaxTokenIssued;

    private IConfigureApiLimits _configureThrottle;
    int _counter;
    private Timer _watchWindowTimer;
    private Timer _restWindowTimer;
    private bool _open;

    public FixWindowTokenRepo(IConfigureApiLimits configureThrottle)
    {
      _configureThrottle = configureThrottle;
      _counter = 0;
      _open = true;
      _watchWindowTimer = new Timer()
      {
        Interval = double.MinValue,
        AutoReset = false,
        Enabled = true,
      };
      _restWindowTimer = new Timer()
      {
        Interval = double.MinValue,
        AutoReset = false,
        Enabled = true
      };
      _watchWindowTimer.Elapsed += WatchWindowExpired;
      _restWindowTimer.Elapsed += RestWindowExpired;
    }
    public string Name => _configureThrottle.Server;

    public TokenInt PullToken(string client)
    {
      var tmpCounter = 0;
      lock (_watchWindowTimer)
      {
        tmpCounter = ++_counter;
      }
      StartWatchTimer();
      var token = GenerateToken((this as ITokenRepository).Name, client, tmpCounter);
      OnTokenIssued(token);
      if (tmpCounter > _configureThrottle.TotalLimit)
        OnMaxTokenIssued(token, tmpCounter);
      return token;
    }

    public void ReturnToken(string tokenId)
    {
      //Do nothing....since this is increment only counter...
    }

    private TokenInt GenerateToken(string server, string client, int tmpCounter)
    {
      return new TokenInt { Client = client, IssuedOn = DateTime.Now, Id = string.Empty, Server = server };
    }

    #region TimerFuncs
    private void RestWindowExpired(object sender, ElapsedEventArgs e)
    {
      _watchWindowTimer.Interval = double.MinValue;
      _watchWindowTimer.Enabled = true;
    }

    private void WatchWindowExpired(object sender, ElapsedEventArgs e)
    {
      //Check the counts. if we are close, raise alarm, if we passed it stop the calls.
      lock (_watchWindowTimer)
      {
        _counter = 0;//reset the counter.
      }
      StartRestTimer();
    }

    private void StartWatchTimer()
    {
      if (_watchWindowTimer.Interval <= 0.0)
      {
        _watchWindowTimer.Enabled = true;
        _watchWindowTimer.Interval = _configureThrottle.WatchDuration.TotalSeconds;
        _watchWindowTimer.Start();
      }
    }

    private void StopWatchTimer()
    {
      _watchWindowTimer.Stop();
      _watchWindowTimer.Enabled = false;
      _watchWindowTimer.Interval = double.MinValue;
    }

    private void StartRestTimer()
    {
      if (_restWindowTimer.Interval <= 0.0)
      {
        _restWindowTimer.Interval = _configureThrottle.RestDuration.TotalSeconds;
        _restWindowTimer.Start();
      }
    }

    private void StopRestTimer()
    {
      _restWindowTimer.Stop();
      _restWindowTimer.Enabled = false;
      _restWindowTimer.Interval = double.MinValue;
    }

    #endregion TimerFuncs

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

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          _watchWindowTimer.Stop();
          _watchWindowTimer.Dispose();

          _restWindowTimer.Stop();
          _restWindowTimer.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~FixWindowTokenRepo()
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
