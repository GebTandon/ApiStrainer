using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TokenDispenser.Services;
using TokenDispenser.Settings;
using TokenGenLib;

namespace TokenDispenser
{
  public class Startup
  {
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddGrpc();
      var tokenMonitor = new RegisterTokenMonitor(services);
      var sec = Configuration.GetSection("ApiLimitSetting");
      services.Configure<ApiLimitSetting>(sec);

      var limitSet = sec.Get<ApiLimitSetting>();
      tokenMonitor.Register(limitSet.ApiServer, new ApiLimits
      {
        Blocking = limitSet.Blocking,
        MaxForDuration = limitSet.MaxForDuration,
        MaxRateLimit = limitSet.MaxRateLimit,
        RestDuration = limitSet.RestDuration,
        WatchDuration = limitSet.WatchDuration
      });

      if (limitSet.MaxForDuration > 0 && limitSet.MaxRateLimit > 0) //injects only 2 ITokenRepository
        services.AddTransient<IGrantToken, MultiTokenApiGateway>();
      else if (limitSet.MaxForDuration > 0 || limitSet.MaxRateLimit > 0) //injects only 1 ITokenRepository
        services.AddTransient<IGrantToken, SingleTokenApiGateway>();
      else
        throw new InvalidOperationException("ITokenRepository cannot be determined, make sure configuration is valid to inject at least 1 ITokenRepository.");
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapGrpcService<TokenGenService>();

        endpoints.MapGet("/", async context =>
              {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
              });
      });
    }
  }
}
