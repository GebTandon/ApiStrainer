using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NuGet.Frameworks;
using TokenGenLib;
using TokenGenLib.Internal;
using Xunit;

namespace TokenGeneratorFixture
{
  public class NonBlockingTokenRepoTests
  {
    Mock<IConfigureApiLimits> _mockApiLimits = null;

    readonly ConcurrentBag<TokenIssuedEventArgs> _callbackTokensIssued = new ConcurrentBag<TokenIssuedEventArgs>();
    readonly ConcurrentBag<MaxTokenIssuedEventArgs> _callbackMaxTokensIssued = new ConcurrentBag<MaxTokenIssuedEventArgs>();
    readonly ConcurrentBag<TokenInt> _tokensIssued = new ConcurrentBag<TokenInt>();

    string _clientName = "xUnitTest";
    const string _serverName = "NonBlock Api";

    [Fact]
    public void GettingTokenOnSingleThreadWorks()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var token = sut.PullToken(randomClientId);
      _tokensIssued.Add(token);

      Task.Delay(TimeSpan.FromSeconds(1));
      Assert.NotNull(token);
      Assert.Equal(randomClientId, token.Client);
      Assert.Empty(_callbackMaxTokensIssued);
      Assert.Single(_callbackTokensIssued);
      Assert.Equal(randomClientId, _callbackTokensIssued.First().Client);
      Assert.Equal(_serverName, token.Server);
      Assert.NotNull(token.Id);
      Assert.NotEqual(Guid.Empty, Guid.Parse(token.Id));
      Assert.True(DateTime.Now - token.IssuedOn > TimeSpan.FromMilliseconds(1));
      Assert.True(DateTime.Now - token.IssuedOn < TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void Rus_2_Threads_And_Issues_2_Tokens_And_Raises_Events_With_Same_TokenId()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount = 2;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullTokenOnAThreadPool_Thread),
            new SutNTokenBag { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent });
        countdownEvent.Wait();
      }

      Assert.Equal(2, _tokensIssued.Count);
      Assert.All(_tokensIssued, x => Assert.NotNull(x));
      Assert.All(_tokensIssued, x => Assert.NotEqual(Guid.Empty, Guid.Parse(x.Id)));
      Assert.All(_tokensIssued, x => Assert.Equal(_clientName, x.Client));
      Assert.All(_tokensIssued, x => Assert.Equal(_serverName, x.Server));

      _tokensIssued.TryTake(out TokenInt token1);
      _tokensIssued.TryTake(out TokenInt token2);
      Assert.Equal(2, _callbackTokensIssued.Count);
      Assert.NotNull(_callbackTokensIssued.Where(x => x.Token.Equals(token1.Id)).FirstOrDefault());
      Assert.NotNull(_callbackTokensIssued.Where(x => x.Token.Equals(token2.Id)).FirstOrDefault());

      Assert.Empty(_callbackMaxTokensIssued);

    }

    [Fact]
    public void Rus_MultipleThreads_And_Issues_MultipleTokens_And_Raises_Events_With_Same_TokenId()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount = 3;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullTokenOnAThreadPool_Thread),
            new SutNTokenBag { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent });
        countdownEvent.Wait();
      }

      Assert.Equal(3, _tokensIssued.Count);
      Assert.Collection(_tokensIssued,
        x => Assert.NotNull(x),
        x => Assert.NotNull(x),
        x => Assert.Null(x)
        );

      _tokensIssued.TryTake(out TokenInt token1);
      _tokensIssued.TryTake(out TokenInt token2);
      Assert.Equal(2, _callbackTokensIssued.Count);
      Assert.NotNull(_callbackTokensIssued.Where(x => x.Token.Equals(token1.Id)).FirstOrDefault());
      Assert.NotNull(_callbackTokensIssued.Where(x => x.Token.Equals(token2.Id)).FirstOrDefault());

      Assert.Single(_callbackMaxTokensIssued);

    }

    private void PullTokenOnAThreadPool_Thread(object sutNTokenBagObj)
    {
      var theObj = sutNTokenBagObj as SutNTokenBag;
      var tmpToken = theObj.TokenRepo.PullToken(_clientName);
      theObj.Tokens.Add(tmpToken);
      theObj.SyncObj.Signal();
    }


    #region EventHanders
    private void Sut_MaxTokenIssued(object sender, MaxTokenIssuedEventArgs e)
    {
      _callbackMaxTokensIssued.Add(e);
    }
    private void Sut_TokenIssued(object sender, TokenIssuedEventArgs e)
    {
      _callbackTokensIssued.Add(e);
    }
    #endregion EventHanders

    private ITokenRepository CreateInstance(string server, int rateLimit, TimeSpan restDuration, TimeSpan watchDuration, int maxTotal, bool sliding)
    {
      _mockApiLimits = new Mock<IConfigureApiLimits>();
      _mockApiLimits.Setup(x => x.Setup(server, rateLimit, restDuration, watchDuration, maxTotal, sliding)).Verifiable();
      _mockApiLimits.SetupGet(c => c.Server).Returns(server);
      _mockApiLimits.SetupGet(c => c.RateLimit).Returns(rateLimit);
      _mockApiLimits.SetupGet(c => c.RestDuration).Returns(restDuration);
      _mockApiLimits.SetupGet(c => c.WatchDuration).Returns(watchDuration);
      _mockApiLimits.SetupGet(c => c.TotalLimit).Returns(maxTotal);

      var sut = new NonBlockingTokenRepo(_mockApiLimits.Object);
      return sut;
    }
  }

  internal class SutNTokenBag
  {
    internal ITokenRepository TokenRepo { get; set; }
    internal ConcurrentBag<TokenInt> Tokens { get; set; }
    internal CountdownEvent SyncObj { get; set; }
  }
}
