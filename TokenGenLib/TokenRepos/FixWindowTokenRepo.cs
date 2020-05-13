using System;
using System.Timers;
using TokenGenLib.Internal;
using Timer = System.Timers.Timer;

namespace TokenGenLib.TokenRepos
{
  /// <summary>
  /// counts number of calls made within fix window, the window starts when first call is made.
  /// </summary>
  public class FixWindowTokenRepo : BaseTokenRepository, ITokenRepository, ILimitWindow, IDisposable
  {
    int _counter;
    private readonly Timer _watchWindowTimer;
    private readonly Timer _restWindowTimer;
    private readonly bool _open;

    public FixWindowTokenRepo(IConfigureApiLimits configureThrottle) : base(configureThrottle)
    {
      _counter = 0;
      _open = true;
      _watchWindowTimer = new Timer(_configureThrottle.WatchDuration.TotalMilliseconds)
      {
        AutoReset = false,
        Enabled = true,
      };
      _restWindowTimer = new Timer(_configureThrottle.RestDuration.TotalMilliseconds)
      {
        AutoReset = false,
        Enabled = true
      };
      _watchWindowTimer.Elapsed += WatchWindowExpired;
      _restWindowTimer.Elapsed += RestWindowExpired;
    }

    public TokenInt PullToken(string client)
    {
      var tmpCounter = 0;
      lock (_watchWindowTimer)
      {
        tmpCounter = ++_counter;
      }
      StartWatchTimer();
      if (tmpCounter > _configureThrottle.TotalLimit)
      {
        OnMaxTokenIssued(client, tmpCounter);
        return null;
      }
      var token = GenerateToken((this as ITokenRepository).Name, client, tmpCounter);
      OnTokenIssued(token);
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
      //since rest timer expired, get ready for next load...
      lock (_watchWindowTimer)
      {
        _counter = 0;//reset the counter.
      }
      //_watchWindowTimer.Interval = double.MinValue;
      //_watchWindowTimer.Enabled = true;
    }

    private void WatchWindowExpired(object sender, ElapsedEventArgs e)
    {
      StartRestTimer();//begin cool down period.
    }

    private void StartWatchTimer()
    {
      if (!_watchWindowTimer.Enabled)
        _watchWindowTimer.Start();
    }

    private void StopWatchTimer()
    {
      if (_watchWindowTimer.Enabled)
        _watchWindowTimer.Stop();
      //_watchWindowTimer.Enabled = false;
      //_watchWindowTimer.Interval = double.MinValue;
    }

    private void StartRestTimer()
    {
      //_restWindowTimer.Interval = _configureThrottle.RestDuration.TotalSeconds;
      if (!_restWindowTimer.Enabled)
        _restWindowTimer.Start();
    }

    private void StopRestTimer()
    {
      if (_restWindowTimer.Enabled)
        _restWindowTimer.Stop();
      //_restWindowTimer.Enabled = false;
      //_restWindowTimer.Interval = double.MinValue;
    }

    #endregion TimerFuncs

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
