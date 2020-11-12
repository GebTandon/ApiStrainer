using System.Diagnostics;
using TokenGenLib;

namespace TokenGen.Ext
{
  public static class ProcessExtractor
  {
    static string _processName = "";
    static readonly Token lockObj = new Token();

    public static string ProcessName()
    {
      if (string.IsNullOrWhiteSpace(_processName))
      {
        lock (lockObj)
        {
          if (string.IsNullOrWhiteSpace(_processName))//check one more time
            _processName = Process.GetCurrentProcess().ProcessName;
        }
      }
      return _processName;
    }
  }
}
