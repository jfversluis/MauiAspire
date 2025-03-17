using CommunityToolkit.Aspire.DevTunnels;

var builder = DistributedApplication.CreateBuilder(args);

var webapi = builder.AddProject<Projects.SampleWebApi>("webapi");

builder.AddProject<Projects.MauiAspire>("mauiapp")
    .WithReference(webapi);

await builder.AddDevTunnels();

builder.Build().Run();
