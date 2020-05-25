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
      if (tokenInt == null) return (Token)NullToken;
      return new Token { Id = tokenInt.Id, IssuedOn = tokenInt.IssuedOn };
    }

    private static TokenInt NullToken = new TokenInt { Client = string.Empty, Id = string.Empty, IssuedOn = DateTime.MinValue, Server = string.Empty };
  }
}