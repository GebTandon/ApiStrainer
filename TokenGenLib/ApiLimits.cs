﻿using System;

namespace TokenGenLib
{
  public class ApiLimits
  {
    public int MaxRateLimit { get; set; }
    public TimeSpan RestDuration { get; set; }
    public TimeSpan WatchDuration { get; set; }
    public int MaxForDuration { get; set; }
    public bool Blocking { get; set; }
  }
}