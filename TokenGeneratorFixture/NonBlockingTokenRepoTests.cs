using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Moq;
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

    readonly string _clientName = "xUnitTest";
    const string _serverName = "NonBlock Api";

    [Fact]
    public void ReturnsTokenForSingleRequest()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;

      var threadCount = 1;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent });
        countdownEvent.Wait();
      }

      var token = _tokensIssued.First();

      Assert.NotNull(token);
      Assert.Equal(_clientName, token.Client);
      Assert.Empty(_callbackMaxTokensIssued);
      Assert.Single(_callbackTokensIssued);
      Assert.Equal(_clientName, _callbackTokensIssued.First().Client);
      Assert.Equal(_serverName, token.Server);
      Assert.NotNull(token.Id);
      Assert.NotEqual(Guid.Empty, Guid.Parse(token.Id));

      /* Time validation does not work, due to how threading works, unless we give large time gaps, 
       * at which point time check is irrelevant.
      Assert.True(DateTime.Now - token.IssuedOn > TimeSpan.FromMilliseconds(1));
      Assert.True(DateTime.Now - token.IssuedOn < TimeSpan.FromMilliseconds(10));
      */
    }

    [Fact]
    public void ReturnsTokensAndRaisesTokenIssuedEventForRequestLessThanLimit()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount = 2;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent, ExtApiCallDuration = TimeSpan.FromMilliseconds(500) });
        countdownEvent.Wait();
      }

      Assert.Equal(2, _tokensIssued.Count);
      Assert.All(_tokensIssued, x => Assert.NotNull(x));
      Assert.All(_tokensIssued, x => Assert.NotEqual(Guid.Empty, Guid.Parse(x.Id)));
      Assert.All(_tokensIssued, x => Assert.Equal(_clientName, x.Client));
      Assert.All(_tokensIssued, x => Assert.Equal(_serverName, x.Server));

      Assert.Equal(2, _callbackTokensIssued.Count);
      Assert.All(_callbackTokensIssued, x => Assert.NotNull(x));
      _tokensIssued.TryTake(out TokenInt token1);
      _tokensIssued.TryTake(out TokenInt token2);
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token1.Id)));
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token2.Id)));

      Assert.Empty(_callbackMaxTokensIssued);

    }

    [Fact]
    public void ReturnsTokenUptoLimitAndThenRaisesMaxTokenIssuedEvent()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount = 3;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent, ExtApiCallDuration = TimeSpan.FromMilliseconds(500) });
        countdownEvent.Wait();
      }

      Assert.Equal(3, _tokensIssued.Count);//3 token issue calls
      Assert.Equal(2, _tokensIssued.Where(x => x != null).Count()); //2 actual tokens 
      Assert.Single(_tokensIssued.Where(x => x == null));//and 1 null

      Assert.Equal(2, _callbackTokensIssued.Count);
      var token1 = _tokensIssued.First(x => x != null);
      var token2 = _tokensIssued.Last(x => x != null);
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token1.Id)));
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token2.Id)));

      Assert.Single(_callbackMaxTokensIssued);//one call was made to MaxLimit reached.
    }

    [Fact]
    public void MultipleCallsUptoLimitWorkAndIssuesTokenIssuedEvent()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount1 = 2;
      using (var countdownEvent1 = new CountdownEvent(threadCount1))
      {
        for (var i = 0; i < threadCount1; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj
            {
              TokenRepo = sut,
              Tokens = _tokensIssued,
              SyncObj = countdownEvent1,
              ExtApiCallDuration = TimeSpan.FromMilliseconds(200)
            });
        countdownEvent1.Wait();
      }

      var threadCount2 = 2;
      using (var countdownEvent2 = new CountdownEvent(threadCount2))
      {
        for (var i = 0; i < threadCount2; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj
            {
              TokenRepo = sut,
              Tokens = _tokensIssued,
              SyncObj = countdownEvent2,
              ExtApiCallDuration = TimeSpan.FromMilliseconds(200)
            });
        countdownEvent2.Wait();
      }

      Assert.Equal(4, _tokensIssued.Count);
      Assert.All(_tokensIssued, (x) => Assert.NotNull(x));

      Assert.Equal(4, _callbackTokensIssued.Count);
      Assert.All(_callbackTokensIssued, (x) => Assert.NotNull(x));
      var token1 = _tokensIssued.First(x => x != null);
      var token4 = _tokensIssued.Last(x => x != null);
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token1.Id)));
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token4.Id)));

      Assert.Empty(_callbackMaxTokensIssued);//one call was made to MaxLimit reached.
    }

    [Fact]
    public void CallsOverLimitWorkAndIssuesTokenIssuedAndMaxTokenIssuedEvent()
    {
      var sut = CreateInstance(_serverName, 2, TimeSpan.MinValue, TimeSpan.MinValue, -1, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount1 = 3;
      using (var countdownEvent1 = new CountdownEvent(threadCount1))
      {
        for (var i = 0; i < threadCount1; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj
            {
              TokenRepo = sut,
              Tokens = _tokensIssued,
              SyncObj = countdownEvent1,
              ExtApiCallDuration = TimeSpan.FromMilliseconds(500)
            });
        countdownEvent1.Wait();
      }

      var threadCount2 = 2;
      using (var countdownEvent2 = new CountdownEvent(threadCount2))
      {
        for (var i = 0; i < threadCount2; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj
            {
              TokenRepo = sut,
              Tokens = _tokensIssued,
              SyncObj = countdownEvent2,
              ExtApiCallDuration = TimeSpan.FromMilliseconds(200)
            });
        countdownEvent2.Wait();
      }

      Assert.Equal(5, _tokensIssued.Count);
      Assert.Single(_tokensIssued.Where(x => x == null));

      Assert.Equal(4, _callbackTokensIssued.Count);
      Assert.All(_callbackTokensIssued, (x) => Assert.NotNull(x));
      var token1 = _tokensIssued.First(x => x != null);
      var token4 = _tokensIssued.Last(x => x != null);
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token1.Id)));
      Assert.Single(_callbackTokensIssued.Where(x => x.Token.Equals(token4.Id)));

      Assert.Single(_callbackMaxTokensIssued);
    }

    private void PullToken_OnAThreadPool_Thread(object sutNTokenBagObj)
    {
      var theObj = sutNTokenBagObj as ThreadFuncObj;
      var tmpToken = theObj.TokenRepo.PullToken(_clientName);
      theObj.Tokens.Add(tmpToken);
      Thread.Sleep(theObj.ExtApiCallDuration);//Simulate external API Call.
      if (tmpToken != null)//null token means token was not issued in this case... so no need to return it.
        theObj.TokenRepo.ReturnToken(tmpToken.Id);
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
      //var mockLogger = new Mock<ILogger<NonBlockingTokenRepo>>();
      var mockLogger = new TestLoggerT<NonBlockingTokenRepo>();
      var sut = new NonBlockingTokenRepo(_mockApiLimits.Object, mockLogger);
      return sut;
    }
  }

  internal class ThreadFuncObj
  {
    internal ITokenRepository TokenRepo { get; set; }
    internal ConcurrentBag<TokenInt> Tokens { get; set; }
    internal CountdownEvent SyncObj { get; set; }
    internal TimeSpan ExtApiCallDuration { get; set; }// Yatin: If tests fail randomly on build server, increase this time in tests... The thread scheduler may be busy.. Taking long time to schedule threadpool threads.
  }
}
