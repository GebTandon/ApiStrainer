using System;

namespace TokenGenLib.Internal
{
  /// <summary>
  /// internal token class.
  /// </summary>
  public partial class TokenInt
  {
    public string Id { get; set; }
    public DateTime IssuedOn { get; set; }
    public string Client { get; set; }
    public string Server { get; set; }
  }

  public partial class TokenInt
  {
    public static explicit operator Token(TokenInt tokenInt)
    {
      return new Token { Id = tokenInt.Id, IssuedOn = tokenInt.IssuedOn };
    }
  }
}