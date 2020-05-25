using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using TokenDispenser.ClientLib;
using TokenDispenser.Protos;

namespace TokenGen.ConsoleApp
{
  static class Program
  {
    const int nmberOfCalls = 20;
    const int simulatedApiDurationInMillSecs = 10;
    const string clientName = "Console Test App";

    static async Task Main(string[] args)
    {
      var clnt = new Client();
      clnt.Initialize();
      var result = await CallExternalApiInParallelAsync(clnt);

      Thread.Sleep(TimeSpan.FromSeconds(10));

      await ReleaseTokensAsync(clnt, result);

      Console.WriteLine("Press return to exit program...");
      Console.ReadLine();
    }

    private static async Task<ObtainTokenReply[]> CallExternalApiInParallelAsync(Client clnt)
    {
      var listOfTokenTasks = new List<Task<ObtainTokenReply>>();
      for (var i = 0; i < nmberOfCalls; i++)
      {
        listOfTokenTasks.Add(Task.Run(async () =>
        {
          ObtainTokenReply obtainTokenReply = await clnt.ObtainToken(new ObtainTokenRequest { Client = clientName });
          await Task.Delay(simulatedApiDurationInMillSecs).ConfigureAwait(false);//emulate calling a WebApi.
          return obtainTokenReply;
        }));
      }
      var result = await Task.WhenAll(listOfTokenTasks).ConfigureAwait(false);
      return result;
    }

    private static async Task ReleaseTokensAsync(Client clnt, IEnumerable<ObtainTokenReply> result)
    {
      var listReleaseTasks = new List<Task<ReleaseTokenReply>>();
      foreach (ObtainTokenReply item in result)
      {
        Console.WriteLine($"Received Token {item.Id} for client {clientName}");
        listReleaseTasks.Add(Task.Run(async () => await clnt.Release(new ReleaseTokenRequest { Clientid = clientName, Tokenid = item.Id })));
      }
      var releaseResults = await Task.WhenAll(listReleaseTasks).ConfigureAwait(false);
      Console.WriteLine($"Released tokens...");
    }
  }
}
