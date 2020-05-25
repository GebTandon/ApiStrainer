using System;
using Microsoft.Extensions.Logging;

namespace TokenGeneratorFixture
{
  public class TestLoggerT<T> : ILogger<T>
  {
    public IDisposable BeginScope<TState>(TState state)
    {
      return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
      
    }

    public void LogInformation(string message, params object[] args)
    {

    }
    public void LogWarninig(string message, params object[] args)
    {

    }
    public void LogException(Exception exception, string message, params object[] args)
    {

    }
  }
}
