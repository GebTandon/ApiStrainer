using System;

namespace TokenGenLib.Internal
{

  public class Stat
  {
    public int CurrentParallel { get; set; }
    public DateTime WindowStart { get; set; }
    public bool IsSliding { get; set; }
    public int CallsInWindow { get; set; }
  }
}
