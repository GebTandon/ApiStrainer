using System;
using System.Data;

namespace TokenGenLib.Internal
{

  public interface IConfigureApiLimits
  {
    string Server { get; }
    int RateLimit { get; }
    int TotalLimit { get; }
    TimeSpan WatchDuration { get; }
    TimeSpan RestDuration { get; }

    public void Setup(string server, int maxRateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxTotalForDuration, bool isBlocking);
  }

  public class ApiLimitsConfig : IConfigureApiLimits
  {
    private string _server;
    private int _rateLimit;
    private TimeSpan _watchDuration;
    private TimeSpan _restDuration;
    private int _maxTotalForDuration;
    private bool _isBlocking;

    public string Server => _server;
    public TimeSpan WatchDuration => _watchDuration;
    public TimeSpan RestDuration => _restDuration;
    public int RateLimit => _rateLimit;
    public int TotalLimit => _maxTotalForDuration;
    public bool IsBlocking => _isBlocking;

    public void Setup(string server, int rateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxTotalForDuration = int.MinValue, bool isBlocking = false)
    {
      _server = server;
      _rateLimit = rateLimit;
      _watchDuration = watchDuration;
      _restDuration = restDuration;
      _maxTotalForDuration = maxTotalForDuration;
      _isBlocking = isBlocking;

      ValidateSetup();
    }

    private void ValidateSetup()
    {
      if (string.IsNullOrWhiteSpace(_server)) throw new DataException("Server name cannot be null or empty..");
      if (_rateLimit < 0 && _maxTotalForDuration < 0) throw new DataException("Either Ratelimit or MaxTotal should be positive value.");
      if(_maxTotalForDuration>0 && _watchDuration.Ticks<=0) throw new DataException("Either watch duration cannot be negative.");
    }
  }
}
