using System;
using System.Threading;
using TokenDispenser.ClientLib;

namespace TokenGen.ConsoleApp
{
  static class Program
  {
    static async System.Threading.Tasks.Task Main(string[] args)
    {
      var clientName = "Console Test App";
      var clnt = new Client();
      clnt.Initialize();
      var token = await clnt.ObtainToken(new TokenDispenser.Protos.ObtainTokenRequest { Client = clientName });
      Console.WriteLine($"Received Token {token.Id} for client {clientName}");
      Thread.Sleep(TimeSpan.FromSeconds(10));
      await clnt.Release(new TokenDispenser.Protos.ReleaseTokenRequest { Clientid = clientName, Tokenid = token?.Id });
      Console.WriteLine($"Released token...");
    }
  }
}
