using System;
using System.Threading.Tasks;

using Grpc.Net.Client;

using TokenDispenser.Protos;

namespace TokenDispenser.ClientLib
{
  public class Client : IDisposable
  {
    private GrpcChannel _channel;

    public void Initialize(string address = "https://localhost:5001")
    {
      _channel = GrpcChannel.ForAddress(address);
    }

    public async Task<ObtainTokenReply> ObtainToken(ObtainTokenRequest request)
    {
      var client = new TokenGen.TokenGenClient(_channel);
      var reply = await client.ObtainAsync(request);
      return reply;
    }

    public async Task<ReleaseTokenReply> Release(ReleaseTokenRequest request)
    {
      var client = new TokenGen.TokenGenClient(_channel);
      var reply = await client.ReleaseAsync(request);
      return reply;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          _channel.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Client()
    // {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
    #endregion
  }
}
