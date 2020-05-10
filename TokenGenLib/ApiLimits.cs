using System;

namespace TokenGenLib
{
  public class ApiLimits
  {
    public int maxRateLimit { get; set; }
    public TimeSpan restDuration { get; set; }
    public TimeSpan watchDuration { get; set; }
    public int maxForDuration { get; set; }
    public bool blocking { get; set; }
  }
}