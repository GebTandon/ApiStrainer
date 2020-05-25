using System;
using System.Timers;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<FixWindowTokenRepo> _logger;

    public FixWindowTokenRepo(IConfigureApiLimits configureThrottle, ILogger<FixWindowTokenRepo> logger) : base(configureThrottle)
    {
      _counter = 0;
      _open = true;
      _logger = logger;
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
        _logger?.LogWarning($"Hit Token Window Limits for Server: {_configureThrottle.Server} Limit:{_configureThrottle.TotalLimit}");
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
      StartWatchTimer();
    }

    private void WatchWindowExpired(object sender, ElapsedEventArgs e)
    {
      _logger?.LogInformation($"    ++Watch timer expired for Server: {_configureThrottle.Server}");
      StartRestTimer();//begin cool down period.
    }

    private void StartWatchTimer()
    {
      _logger?.LogInformation($"    ++Starting watch timer for Server: {_configureThrottle.Server}");
      //if (!_watchWindowTimer.Enabled)
        _watchWindowTimer.Start();
    }

    private void StopWatchTimer()
    {
      _logger?.LogInformation($"    ++Watch timer expired for Server: {_configureThrottle.Server}");
      //if (_watchWindowTimer.Enabled)
        _watchWindowTimer.Stop();
    }

    private void StartRestTimer()
    {
      _logger?.LogInformation($"    ++Starting rest timer for Server: {_configureThrottle.Server}");
      //_restWindowTimer.Interval = _configureThrottle.RestDuration.TotalSeconds;
      //if (!_restWindowTimer.Enabled)
        _restWindowTimer.Start();
    }

    private void StopRestTimer()
    {
      _logger?.LogInformation($"    ++Rest timer expired for Server: {_configureThrottle.Server}");
      //if (_restWindowTimer.Enabled)
        _restWindowTimer.Stop();
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
