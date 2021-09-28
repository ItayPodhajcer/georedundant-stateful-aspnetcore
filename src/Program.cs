using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNext.Net.Cluster.Consensus.Raft.Http.Embedding;
using DotNext.Net.Cluster.Consensus.Raft.Http;
using DotNext.Net.Cluster.Consensus.Raft;
using Microsoft.AspNetCore.Routing;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureWebHostDefaults(webBuilder =>
{
  webBuilder.Configure(app => app
    .UseConsensusProtocolHandler()
    .MapWhen(
      context => context.Request.Method != "GET",
      appBuilder => appBuilder
        .RedirectToLeader(string.Empty)
        .UseRouting()
        .UseEndpoints(MapWriteEndpoints))
    .UseRouting()
    .UseEndpoints(MapReadEndpoints));

  webBuilder.ConfigureServices(services =>
  {
    services
      .AddRouting();
  });
});

builder.JoinCluster();

var app = builder.Build();

await app.RunAsync();

void MapWriteEndpoints(IEndpointRouteBuilder endpoints) => 
  endpoints
    .MapPut("/{id}", async (context) =>
    {
      var cluster = context.RequestServices.GetRequiredService<IRaftCluster>();

      await context.Response.WriteAsJsonAsync(new
      {
        Leader = cluster.Leader?.EndPoint.ToString(),
        Current = context.Request.Host.Value
      });
    });

void MapReadEndpoints(IEndpointRouteBuilder endpoints) => 
  endpoints
    .MapGet("/", async (context) =>
    {
      var cluster = context.RequestServices.GetRequiredService<IRaftCluster>();

      await context.Response.WriteAsJsonAsync(new
      {
        Leader = cluster.Leader?.EndPoint.ToString(),
        Current = context.Request.Host.Value
      });
    });