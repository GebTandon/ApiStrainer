using System;
using System.Threading;
using TokenDispenser.ClientLib;

namespace TokenGen.ConsoleApp
{
  static class Program
  {
    static async System.Threading.Tasks.Task Main(string[] args)
    {
      var clnt = new Client();
      clnt.Initialize();
      var token = await clnt.ObtainToken(new TokenDispenser.Protos.ObtainTokenRequest { Client = "Console Test App" });
      Thread.Sleep(TimeSpan.FromSeconds(10));
      await clnt.Release(new TokenDispenser.Protos.ReleaseTokenRequest { Clientid = "Console Test App", Tokenid = token?.Id });
    }
  }
}
