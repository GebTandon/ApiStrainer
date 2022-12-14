using System.Threading.Tasks;

using TokenDispenser.ClientLib;
//using clientProto = TokenDispenser.Protos.ObtainTokenRequest;
using Xunit;

namespace TokenDispenser.Integration.Fixture
{
  public class SimpleTests:IClassFixture<TestServerFixture>
  {
    private readonly TestServerFixture _fixture;

    public SimpleTests(TestServerFixture fixture)
    {
      _fixture = fixture;
    }

    [Fact]
    public async Task FirstIntegrationTest()
    {
      using var grpcClient = new Client();
      grpcClient.Initialize(_fixture.Client, _fixture.Client.BaseAddress.ToString());
      var tokenReqst = new Protos.ObtainTokenRequest();
      var respoonse = await grpcClient.ObtainToken(tokenReqst).ConfigureAwait(false);
      Assert.NotNull(respoonse);
    }
  }
}
