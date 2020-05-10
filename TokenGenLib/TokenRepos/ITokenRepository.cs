using System;

namespace TokenGenLib.Internal
{
  public interface ITokenRepository
  {
    public string Name { get; }
    public TokenInt PullToken(string client);
    public void ReturnToken(string tokenId);
    public event EventHandler<TokenIssuedEventArgs> TokenIssued;
    public event EventHandler<MaxTokenIssuedEventArgs> MaxTokenIssued;
  }
  public interface ILimitRate { }
  public interface ILimitWindow { }

}
