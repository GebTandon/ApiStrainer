using System;

namespace TokenGenLib.Internal
{

  public static class AsyncEventsHelper
  {
    public static void RaiseEventAsync<T>(EventHandler<T> h, object sender, T e) where T : EventArgs
    {
      if (h != null)
      {
        var delegates = h.GetInvocationList();
        for (var i = 0; i < delegates.Length; i++)
          ((EventHandler<T>)delegates[i]).BeginInvoke(sender, e, h.EndInvoke, null);
      }
    }
  }
}
