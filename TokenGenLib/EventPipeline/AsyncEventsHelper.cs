using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TokenGenLib.Internal
{

  public static class AsyncEventsHelper
  {
    public static void RaiseEventAsync<T>(EventHandler<T> h, object sender, T e) where T : EventArgs
    {
      try
      {
        if (h != null)
        {
          var delegates = h.GetInvocationList();
#if NETFRAMEWORK
          ((EventHandler<T>)delegates[i]).BeginInvoke(sender, e, h.EndInvoke, null);
#elif NETCOREAPP || NETSTANDARD
          var tasks = new List<Task>();
          for (var i = 0; i < delegates.Length; i++)
          {
            var evntHandler = (EventHandler<T>)delegates[i];
            var tsk = Task.Run(() => evntHandler(sender, e));
            tasks.Add(tsk);
          }
          Task.WaitAll(tasks.ToArray());
#endif
        }
      }
      catch (Exception ex)
      {
        // GTan: Keep quiet
        //throw;
      }
    }
  }
}
