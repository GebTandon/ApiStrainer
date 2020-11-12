using System;
using System.IO;
using System.Net.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace TokenDispenser.Integration.Fixture
{
  //https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1#:~:text=ASP.NET%20Core%20integration%20tests,and%20responses%20with%20the%20SUT.
  //https://fullstackmark.com/post/20/painless-integration-testing-with-aspnet-core-web-api
  //https://adamstorr.azurewebsites.net/blog/integration-testing-with-aspnetcore-3-1
  //https://github.com/kkoziarski/grpc-dotnet-enterprise
  //https://github.com/grpc/grpc-dotnet/issues/336
  //https://trafficparrot.com/tutorials/mocking-and-simulating-grpc.html
  public class TestServerFixture
  {
    private readonly IHost _testServer;
    public HttpClient Client { get; }

    public TestServerFixture()
    {
      var builder = new HostBuilder()
        .ConfigureWebHost(webHost =>
        {
          webHost.UseTestServer()
          .UseEnvironment("Integration")
          .UseStartup<Startup>()
          .ConfigureTestServices((services) =>
          {
          })
          //.Configure(app => app.Run(async ctx => await ctx.Response.WriteAsync("Hello World !!")))
          ;
        });
      _testServer = builder.StartAsync().GetAwaiter().GetResult();
      Client = _testServer.GetTestClient();

    }

    private string GetWebRootPath()
    {

      var testAssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      // Remove the "\\bin\\Debug\\netcoreapp2.1"
      var projectPath = Directory.GetParent(testAssemblyPath.Substring(0, testAssemblyPath.LastIndexOf(@"\bin\", StringComparison.Ordinal))).FullName;

      //var testProjectPath = PlatformServices.Default.Application.ApplicationBasePath;
      return Path.Combine(projectPath, "wwwroot");
    }

    public void Dispose()
    {
      Client.Dispose();
      _testServer.Dispose();
    }
  }
}
