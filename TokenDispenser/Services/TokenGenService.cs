using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using TokenDispenser.Protos;
using TokenGenLib;

namespace TokenDispenser.Services
{
  public class TokenGenService : TokenGen.TokenGenBase
  {
    private ILogger<TokenGenService> _logger;
    private IGrantToken _tokenGen;

    public TokenGenService(IGrantToken tokenGen, ILogger<TokenGenService> logger)
    {
      _logger = logger;
      _tokenGen = tokenGen;
    }

    public override Task<ObtainTokenReply> Obtain(ObtainTokenRequest request, ServerCallContext context)
    {
      try
      {
        var token = _tokenGen.Obtain(request.Client);
        //Console.WriteLine($"Generated Token:- Id:{token.Id} IssuedOn:{token.IssuedOn.ToLongTimeString()} Client:{request.Client}");
        _logger?.LogInformation($"Generated Token:- Id:{token?.Id} IssuedOn:{token?.IssuedOn.ToLongTimeString()} Client:{request.Client}");
        return Task.FromResult(new ObtainTokenReply { Id = token?.Id, Issuedon = Timestamp.FromDateTime(token?.IssuedOn.ToUniversalTime()??DateTime.MinValue) });
      }
      catch (Exception ex)
      {
        _logger?.LogError(ex, $"Exception obtaining token client:{request.Client}");
        throw;
      }
    }

    public override Task<ReleaseTokenReply> Release(ReleaseTokenRequest request, ServerCallContext context)
    {
      try
      {
        _tokenGen.Release(request.Clientid, request.Tokenid);
        //Console.WriteLine($"Release token for Client: {request.Clientid} TokenId:{request.Tokenid}");
        _logger?.LogInformation($"Release token for Client: {request.Clientid} TokenId:{request.Tokenid}");
        return Task.FromResult(new ReleaseTokenReply());
      }
      catch (Exception ex)
      {
        _logger?.LogError(ex, $"Exception releasing token for client:{request.Clientid}, token:{request.Tokenid}");
        throw;
      }
    }
  }
}
