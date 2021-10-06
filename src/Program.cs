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
using Microsoft.Extensions.Configuration;
using System;

var builder = Host.CreateDefaultBuilder(args);

var app = builder
  .ConfigureWebHostDefaults(webBuilder => webBuilder
    .Configure(app => app
      .UseConsensusProtocolHandler()
      .MapWhen(
        context => context.Request.Method != "GET",
        appBuilder => appBuilder
          .RedirectToLeader(string.Empty)
          .UseRouting()
          .UseEndpoints(MapWriteEndpoints))
      .UseRouting()
      .UseEndpoints(MapReadEndpoints))
    .ConfigureAppConfiguration((app, config) => config
      .AddCommandLine(args))
    .ConfigureServices(services => services
      .AddRouting()
      .UsePersistenceEngine<IServiceState, ServiceState>()))
  .JoinCluster()
  .Build();

await app.RunAsync();

void MapWriteEndpoints(IEndpointRouteBuilder endpoints) =>
  endpoints
    .MapPut("/{id:int}", async (context) =>
    {
      var id = int.Parse(context.Request.RouteValues["id"].ToString());
      var requestBody = await context.Request.ReadFromJsonAsync<PutStateRequest>();
      var serviceState = context.RequestServices.GetRequiredService<IServiceState>();

      await serviceState.SetStateAsync(id, requestBody.State);

      var cluster = context.RequestServices.GetRequiredService<IRaftCluster>();

      await context.Response.WriteAsJsonAsync(new PutStateResponse
      (
        Leader: cluster.Leader?.EndPoint.ToString(),
        Current: context.Request.Host.Value,
        Id: id,
        State: requestBody.State
      ));
    });

void MapReadEndpoints(IEndpointRouteBuilder endpoints) =>
  endpoints
    .MapGet("/{id:int}", async (context) =>
    {
      var id = int.Parse(context.Request.RouteValues["id"].ToString());
      var serviceState = context.RequestServices.GetRequiredService<IServiceState>();

      var state = await serviceState.GetStateAsync(id);

      var cluster = context.RequestServices.GetRequiredService<IRaftCluster>();

      await context.Response.WriteAsJsonAsync(new GetStateResponse
      (
        Leader: cluster.Leader?.EndPoint.ToString(),
        Current: context.Request.Host.Value,
        Id: id,
        State: state
      ));
    });

record GetStateResponse(string Leader, string Current, int Id, string State);
record PutStateRequest(string State);
record PutStateResponse(string Leader, string Current, int Id, string State);