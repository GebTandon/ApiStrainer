using System;

namespace TokenGen.ConsoleApp.Settings
{
  public class ApiLimitSetting
  {
    public int MaxRateLimit { get; set; }
    public TimeSpan RestDuration { get; set; }
    public TimeSpan WatchDuration { get; set; }
    public int MaxForDuration { get; set; }
    public bool Blocking { get; set; }
    public string ApiServer { get; set; }
  }
}
