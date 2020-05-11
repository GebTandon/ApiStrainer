using System;

namespace TokenGenLib.Internal
{

  public interface IConfigureApiLimits
  {
    string Server { get; }
    int RateLimit { get; }
    int TotalLimit { get; }
    TimeSpan WatchDuration { get; }
    TimeSpan RestDuration { get; }

    public void Setup(string server, int maxRateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxTotalForDuration, bool isSliding);
  }

  public class ApiLimitsConfig : IConfigureApiLimits
  {
    private string _server;
    private int _rateLimit;
    private TimeSpan _watchDuration;
    private TimeSpan _restDuration;
    private int _maxTotalForDuration;
    private bool _isSliding;

    public string Server => _server;
    public TimeSpan WatchDuration => _watchDuration;
    public TimeSpan RestDuration => _restDuration;
    public int RateLimit => _rateLimit;
    public int TotalLimit => _maxTotalForDuration;

    public void Setup(string server, int rateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxTotalForDuration = int.MinValue, bool isSliding = false)
    {
      _server = server;
      _rateLimit = rateLimit;
      _watchDuration = watchDuration;
      _restDuration = restDuration;
      _maxTotalForDuration = maxTotalForDuration;
      _isSliding = isSliding;

      ValidateSetup();
    }

    private void ValidateSetup()
    {
      //Yatin: Validate the setup values are correct.
    }
  }
}
