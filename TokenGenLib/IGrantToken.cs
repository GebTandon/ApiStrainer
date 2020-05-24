using System.Collections.Generic;
using TokenGenLib.Internal;

namespace TokenGenLib
{

  public interface IGrantToken
  {
    public Token Obtain(string client);
    public void Release(string client, string tokenId);
  }

  public class MultiTokenApiGateway : IGrantToken
  {
    private readonly IList<ITokenRepository> _tokenRepos;
    private readonly ITokenRepository _iLimitRate;//Only tokens obtained from RateLimit needs to be returned.
    private readonly ITokenRepository _iLimitWindow;//Token needs to be issued, but not returned..

    public MultiTokenApiGateway(IList<ITokenRepository> tokenRepos)
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

    public Token Obtain(string client)
    {
      var rateLimitToken = (Token)_iLimitRate.PullToken(client);
      _iLimitWindow.PullToken(client); // Yatin: this token is not required to be returned.
      return rateLimitToken;
    }

    public void Release(string client, string tokenId)
    {
      _iLimitRate.ReturnToken(tokenId);
    }
  }


  public class SingleTokenApiGateway : IGrantToken
  {
    private readonly ITokenRepository _tokenRepo;

    public SingleTokenApiGateway(ITokenRepository tokenRepo)
    {
      _tokenRepo = tokenRepo;
    }

    public Token Obtain(string client)
    {
      var retVal = (Token)_tokenRepo.PullToken(client);
      _tokenRepo.PullToken(client);
      return retVal;
    }

    public void Release(string client, string tokenId)
    {
      _tokenRepo.ReturnToken(tokenId);
    }
  }
}
