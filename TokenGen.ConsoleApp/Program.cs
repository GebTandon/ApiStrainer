using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenDispenser.ClientLib;
using TokenDispenser.Protos;

namespace TokenGen.ConsoleApp
{
  static class Program
  {
    static async Task Main(string[] args)
    {
      const int nmberOfCalls = 20;
      const int simulatedApiDurationInMillSecs = 10;
      var clientName = "Console Test App";
      var clnt = new Client();
      clnt.Initialize();
      var listOfTokenTasks = new List<Task<ObtainTokenReply>>();
      for (var i = 0; i < nmberOfCalls; i++)
      {
        listOfTokenTasks.Add(Task.Run(async () =>
        {
          ObtainTokenReply obtainTokenReply = await clnt.ObtainToken(new ObtainTokenRequest { Client = clientName });
          await Task.Delay(simulatedApiDurationInMillSecs);
          return obtainTokenReply;
        }));
      }
      var result = await Task.WhenAll(listOfTokenTasks).ConfigureAwait(false);
      Thread.Sleep(TimeSpan.FromSeconds(10));
      var listReleaseTasks = new List<Task<ReleaseTokenReply>>();
      foreach (var item in result)
      {
        Console.WriteLine($"Received Token {item.Id} for client {clientName}");
        listReleaseTasks.Add(Task.Run(async () => await clnt.Release(new ReleaseTokenRequest { Clientid = clientName, Tokenid = item.Id })));
      }
      var releaseResults = await Task.WhenAll(listReleaseTasks).ConfigureAwait(false);
      foreach (var item in releaseResults)
      {
        Console.WriteLine($"Released token...");
      }

      Console.WriteLine("Press return to exit program...");
      Console.ReadLine();
    }
  }
}
