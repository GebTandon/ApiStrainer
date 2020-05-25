using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Moq;
using TokenGenLib.Internal;
using TokenGenLib.TokenRepos;
using Xunit;

namespace TokenGeneratorFixture
{
  public class FixWindowTokenRepoTests
  {
    Mock<IConfigureApiLimits> _mockApiLimits = null;

    readonly ConcurrentBag<TokenIssuedEventArgs> _callbackTokensIssued = new ConcurrentBag<TokenIssuedEventArgs>();
    readonly ConcurrentBag<MaxTokenIssuedEventArgs> _callbackMaxTokensIssued = new ConcurrentBag<MaxTokenIssuedEventArgs>();
    readonly ConcurrentBag<TokenInt> _tokensIssued = new ConcurrentBag<TokenInt>();

    readonly string _clientName = "xUnitTest";
    const string _serverName = "FixWindow Api";

    [Fact]
    public void ReturnsTokenForSingleRequest()
    {
      var sut = CreateInstance(_serverName, 0, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2), 5, false);
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
      Assert.Equal(string.Empty, token.Id);

      /* Time validation does not work, due to how threading works, unless we give large time gaps, 
       * at which point time check is irrelevant.
      Assert.True(DateTime.Now - token.IssuedOn > TimeSpan.FromMilliseconds(1));
      Assert.True(DateTime.Now - token.IssuedOn < TimeSpan.FromMilliseconds(10));
      */
    }

    [Fact]
    public void ReturnsTokensAndRaisesTokenIssuedEventForRequestWithinWindowLimit()
    {
      var sut = CreateInstance(_serverName, 0, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2), 5, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount = 5;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent, ExtApiCallDuration = TimeSpan.FromMilliseconds(500) });
        countdownEvent.Wait();
      }

      Assert.Equal(5, _tokensIssued.Count);
      Assert.All(_tokensIssued, x => Assert.NotNull(x));
      Assert.All(_tokensIssued, x => Assert.Equal(string.Empty, x.Id));
      Assert.All(_tokensIssued, x => Assert.Equal(_clientName, x.Client));
      Assert.All(_tokensIssued, x => Assert.Equal(_serverName, x.Server));

      Assert.Equal(5, _callbackTokensIssued.Count);
      Assert.All(_callbackTokensIssued, x => Assert.NotNull(x));

      Assert.Empty(_callbackMaxTokensIssued);

    }

    [Fact]
    public void ReturnsTokenUptoLimitAndThenRaisesMaxTokenIssuedEvent()
    {
      var sut = CreateInstance(_serverName, 0, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50), 5, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount = 10;
      using (var countdownEvent = new CountdownEvent(threadCount))
      {
        for (var i = 0; i < threadCount; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj { TokenRepo = sut, Tokens = _tokensIssued, SyncObj = countdownEvent, ExtApiCallDuration = TimeSpan.FromMilliseconds(500) });
        countdownEvent.Wait();
      }

      Assert.Equal(10, _tokensIssued.Count);
      Assert.True(_tokensIssued.Count(x => x != null) > 0); //a few tokens were issues
      Assert.True(_tokensIssued.Count(x => x == null) > 0); //a few failed too.

      Assert.True(_callbackTokensIssued.Count > 0);
      Assert.True(_callbackMaxTokensIssued.Count > 0);
    }

    [Fact]
    public void MultipleCallsBeyondWindowFailAndRaiseTokenIssuedAndMaxTokenIssuedEvents()
    {
      var sut = CreateInstance(_serverName, 0, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100), 5, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount1 = 20;
      using (var countdownEvent1 = new CountdownEvent(threadCount1))
      {
        for (var i = 0; i < threadCount1; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj
            {
              TokenRepo = sut,
              Tokens = _tokensIssued,
              SyncObj = countdownEvent1,
              ExtApiCallDuration = TimeSpan.FromMilliseconds(400)
            });
        countdownEvent1.Wait();
      }

      var threadCount2 = 10;
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

      var tokenIssuedEventCount = _callbackTokensIssued.Count;
      var maxTokenIssuedEventCount = _callbackMaxTokensIssued.Count;
      Assert.True(_tokensIssued.Count == 20 + 10);//30 token-issue calls were made
      Assert.True(_tokensIssued.Count(x => x != null) > 0); //a few tokens were issues
      Assert.True(_tokensIssued.Count(x => x == null) > 0); //a few failed too.
      Assert.Equal(20 + 10, maxTokenIssuedEventCount + tokenIssuedEventCount);
      Assert.True(tokenIssuedEventCount < 20 + 10);
      Assert.True(maxTokenIssuedEventCount > 0);
    }

    [Fact]
    public void MultipleCallsBeyondWindowFailAndRaiseTokenIssuedAndMaxTokenIssuedEvents_RestTimeDoesResetCounts()
    {
      var sut = CreateInstance(_serverName, 0, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100), 5, false);
      sut.TokenIssued += Sut_TokenIssued;
      sut.MaxTokenIssued += Sut_MaxTokenIssued;
      var randomClientId = Guid.NewGuid().ToString();

      var threadCount1 = 20;
      using (var countdownEvent1 = new CountdownEvent(threadCount1))
      {
        for (var i = 0; i < threadCount1; i++)
          ThreadPool.QueueUserWorkItem(new WaitCallback(PullToken_OnAThreadPool_Thread),
            new ThreadFuncObj
            {
              TokenRepo = sut,
              Tokens = _tokensIssued,
              SyncObj = countdownEvent1,
              ExtApiCallDuration = TimeSpan.FromMilliseconds(400)
            });
        countdownEvent1.Wait();
      }
      var b4_tokensIssuedCalls = _tokensIssued.Count;
      var b4_tokensIssuedSuccessfully = _tokensIssued.Count(x => x != null);
      var b4_tokenIssuedEventCount = _callbackTokensIssued.Count;
      var b4_maxTokenIssuedEventCount = _callbackMaxTokensIssued.Count;

      Thread.Sleep(TimeSpan.FromMilliseconds(20 + 100));//wait for one cycle
      var threadCount2 = 5;
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

      var a8_tokensIssuedCalls = _tokensIssued.Count - b4_tokensIssuedCalls;
      var a8_tokensIssuedSuccessfully = _tokensIssued.Count(x => x != null) - b4_tokensIssuedSuccessfully;
      var a8_tokenIssuedEventCount = _callbackTokensIssued.Count - b4_tokenIssuedEventCount;
      var a8_maxTokenIssuedEventCount = _callbackMaxTokensIssued.Count - b4_maxTokenIssuedEventCount;

      Assert.True(b4_tokensIssuedCalls == 20);//20 calls issued before..
      Assert.True(_tokensIssued.Count(x => x != null) > 0);//few success
      Assert.True(_tokensIssued.Count(x => x == null) > 0);//few failed
      Assert.Equal(20, b4_tokenIssuedEventCount + b4_maxTokenIssuedEventCount);
      Assert.True(b4_tokenIssuedEventCount < 20);
      Assert.True(b4_maxTokenIssuedEventCount > 0);

      Assert.Equal(5, a8_tokensIssuedSuccessfully);//shows that the 2set of 5 calls, none were failures, since the timers were reset.
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
      //var mockLogger = new Mock<ILogger<FixWindowTokenRepo>>();
      var mockLogger = new TestLoggerT<FixWindowTokenRepo>();
      var sut = new FixWindowTokenRepo(_mockApiLimits.Object, mockLogger);
      return sut;
    }
  }

  //internal class ThreadFuncObj
  //{
  //  internal ITokenRepository TokenRepo { get; set; }
  //  internal ConcurrentBag<TokenInt> Tokens { get; set; }
  //  internal CountdownEvent SyncObj { get; set; }
  //  internal TimeSpan ExtApiCallDuration { get; set; }// Yatin: If tests fail randomly on build server, increase this time in tests... The thread scheduler may be busy.. Taking long time to schedule threadpool threads.
  //}

}
