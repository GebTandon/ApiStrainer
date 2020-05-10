using System.Collections.Generic;
using TokenGenLib.Internal;

namespace TokenGenLib
{
  public interface IGrantToken
  {
    public IList<Token> Obtain(string client);
    public void Release(string client, IList<string> tokenIds);
  }

  public class ApiGateway : IGrantToken
  {
    private IList<ITokenRepository> _tokenRepos;
    private ITokenRepository _iLimitRate;
    private ITokenRepository _iLimitWindow;

    public ApiGateway(IList<ITokenRepository> tokenRepos)
    {
      _tokenRepos = tokenRepos;
      if (tokenRepos[0] is ILimitRate)
        _iLimitRate = tokenRepos[0];
      else
        _iLimitWindow = tokenRepos[0];
      if (tokenRepos[1] is ILimitWindow)
        _iLimitWindow = tokenRepos[1];
      else
        _iLimitRate = tokenRepos[1];
    }
    public IList<Token> Obtain(string client)
    {
      var retVal = new List<Token>();
      foreach (var tokenRepo in _tokenRepos)
        retVal.Add((Token)tokenRepo.PullToken(client));
      return retVal;
    }

    public void Release(string client, IList<string> tokenIds)
    {
      foreach (var tokenId in tokenIds)
      {
        foreach (var tokenrepo in _tokenRepos)
          tokenrepo.ReturnToken(tokenId);
      }
    }
  }
}
