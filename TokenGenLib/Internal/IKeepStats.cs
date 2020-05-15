using System;
using System.Collections.Generic;

namespace TokenGenLib.Internal
{

  public interface IKeepStats
  {
    void UpdateStats(int tokensCount);
    Stat Report(TimeSpan timeSpan);
  }

  public class StatsKeeper : IKeepStats, IDisposable
  {
    private readonly IList<ITokenRepository> _tokenRepos; // Yatin: this will fail if we inject only one ITokenRepository, so think of using IActivator to get services instead of constructor injection.

    public StatsKeeper(IList<ITokenRepository> tokenRepos)
    {
      _tokenRepos = tokenRepos;
      //_tokenRepo.TokenIssued += UpdateStats; // Yatin: Need to fix this..
    }

    private void UpdateStats(object sender, TokenIssuedEventArgs e)
    {
      // Yatin: Update public statistics.
    }

    public void UpdateStats(int tokensCount)
    {
      // Yatin: Not Used, may need to remove the method.
    }


    public Stat Report(TimeSpan timeSpan)
    {
      return new Stat();
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          //_tokenRepo.TokenIssued -= UpdateStats; // Yatin: Fix this too.
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~StatsKeeper()
    // {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
