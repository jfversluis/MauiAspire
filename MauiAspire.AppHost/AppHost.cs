var builder = DistributedApplication.CreateBuilder(args);

var webapi = builder.AddProject<Projects.SampleWebApi>("webapi");

var mauiapp = builder.AddMauiProject("mauiapp", "../SampleMauiApp/SampleMauiApp.csproj");

mauiapp.AddWindowsDevice()
    .WithReference(webapi);

mauiapp.AddMacCatalystDevice()
    .WithReference(webapi);

builder.Build().Run();
