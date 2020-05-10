using System;

namespace TokenGenLib.Internal
{
  public class TokenIssuedEventArgs : EventArgs
  {
    public string Token { get; set; }
    public string Client { get; set; }
    public DateTime Time { get; set; }
  }

  public class MaxTokenIssuedEventArgs : EventArgs
  {
    public int Counts { get; set; }
    public string Client { get; set; }
    public DateTime Time { get; set; }
  }
}
