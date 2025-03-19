# .NET MAUI <3 .NET Aspire

This repository serves as a proof-of-concept for what a .NET MAUI integration with .NET Aspire would look like.

You can either pull down this repository which should be a self-contained sample that you can play with. Additionally, you can try to apply this to an existing .NET MAUI project that you may want to try this on. See the steps for that below.

## Prerequisites

You will need to run all of the below with Visual Studio 17.13+ on Windows for the time being

## Applying to your existing .NET MAUI project

* Get the .NET MAUI artifacts from: https://github.com/dotnet/maui/pull/24365
  * There is a version also [included](https://github.com/jfversluis/MauiAspire/tree/main/nuget) already in this repository. Get the artifacts from here or the above PR
* Setup a local NuGet feed with the artifacts above and make sure your .NET MAUI project references this version and is able to restore these packages
* Go into the .NET MAUI app [csproj](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L4) and change `<TargetFrameworks>` to `<TargetFramework>` (no trailing s) and pick the framework you want
  * This is due to a bug in the tooling, if this were to be an actual project in the future, this will be resolved
* Still in the .NET MAUI project csproj add (without this, service discovery will still work, all the rest will **not** work), see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L43-L46) in this sample project:
  ```xml
  <PropertyGroup Condition="($([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' or $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios') and '$(Configuration)' == 'Debug'">
    <HttpActivityPropagationSupport>true</HttpActivityPropagationSupport>
  </PropertyGroup>
  ```
  
* Still in the .NET MAUI project csproj add (without this metrics will not work for Android, the rest **does** work), see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L48-L51) in this sample project:
  ```xml
  <!-- Needed for enabling metrics on Android -->
  <PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' and '$(Configuration)' == 'Debug'">
      <MetricsSupport>true</MetricsSupport>
  </PropertyGroup>
  ```
  
* Add .NET MAUI app project reference to .NET Aspire HostApp project, see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire.AppHost/MauiAspire.AppHost.csproj#L19) in this sample project.
* Add your .NET MAUI app to the .NET Aspire AppHost [**Program.cs**](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire.AppHost/Program.cs) and add any references to services that you want to consume in your .NET MAUI app. For example:
  ```csharp
  // This was probably already there
  var builder = DistributedApplication.CreateBuilder(args);

  // This is a service you want to call from your .NET MAUI app and/or other projects
  var webapi = builder.AddProject<Projects.SampleWebApi>("webapi");

  // This registers your .NET MAUI app and adds a reference to the Web API you want to call
  builder.AddProject<Projects.MauiAspire>("mauiapp")
      .WithReference(webapi);

  await builder.WithDevTunnels();

  // This was probably already there
  builder.Build().Run();
  ```
* Add .NET MAUI ServiceDefaults project to the solution
  * This is a new template in the artifacts above, or take it from: https://github.com/jfversluis/MauiAspire/tree/main/MauiApp.ServiceDefaults
* Add a [reference](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L77) to the .NET MAUI ServiceDefaults project in your .NET MAUI app
* Go into your [**MauiProgram.cs**](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiProgram.cs#L19) in your .NET MAUI project and add `builder.AddServiceDefaults();` also make sure to add `using Microsoft.Extensions.Hosting;` in the top of your file.
* Start registering your services/HttpClient instances, for example (of course make sure that your service discovery endpoints match, in this case `webapi` must match how your Web API is registered in the .NET Aspire AppHost project), see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiProgram.cs#L21-L26) in this sample project:
  ```csharp
  builder.Services.AddHttpClient<WeatherApiClient>(client =>
  {
      // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
      // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
      client.BaseAddress = new("https+http://webapi");
  });
  ```
* Go into the .NET Aspire AppHost project's **launchSettings.json** file and note the port number for [`DOTNET_DASHBOARD_OTLP_ENDPOINT_URL`](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire.AppHost/Properties/launchSettings.json#L13) (for example 21104)
* Go into the Web API [**launchSettings.json**](https://github.com/jfversluis/MauiAspire/blob/main/SampleWebApi/Properties/launchSettings.json#L17) file and note the port number of the application. (for example 7291)
  * In this example I assume we're only using the https one. The http endpoint should work as well, you will need to note this port too and add it to the Dev Tunnel command a little later.
* Make sure Dev Tunnels is installed: https://learn.microsoft.com/azure/developer/dev-tunnels/get-started and you are logged in
* Consume your registered service/HttpClient in your .NET MAUI app
* Set the AppHost project as the startup project, click the run button and all should light up and you should see all data in the .NET Aspire dashboard!

## Setting things up from scratch

* Get the .NET MAUI artifacts from: https://github.com/dotnet/maui/pull/24365
  * There is a version also [included](https://github.com/jfversluis/MauiAspire/tree/main/nuget) already in this repository, so you can skip this step if you're going to look at this project. If you want to apply it to your own project, get the artifacts from here or the above PR.
* Setup a local NuGet feed with the artifacts above
* Create a new solution with the .NET Aspire AppHost project and whatever you desire or add the Aspire AppHost project to your existing .NET MAUI project/solution
* Add a .NET MAUI app and make sure that the .NET MAUI NuGet reference matches the version from the local NuGet feed above
* Go into the .NET MAUI app [csproj](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L4) and change `<TargetFrameworks>` to `<TargetFramework>` (no trailing s) and pick the framework you want
  * This is due to a bug in the tooling, if this were to be an actual project in the future, this will be resolved
* Still in the .NET MAUI project csproj add (without this, service discovery will still work, all the rest will **not** work), see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L43-L46) in this sample project:
  ```xml
  <PropertyGroup Condition="($([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' or $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios') and '$(Configuration)' == 'Debug'">
    <HttpActivityPropagationSupport>true</HttpActivityPropagationSupport>
  </PropertyGroup>
  ```
  
* Still in the .NET MAUI project csproj add (without this metrics will not work for Android, the rest **does** work), see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L48-L51) in this sample project:
  ```xml
  <!-- Needed for enabling metrics on Android -->
  <PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' and '$(Configuration)' == 'Debug'">
      <MetricsSupport>true</MetricsSupport>
  </PropertyGroup>
  ```
  
* Add .NET MAUI app project reference to .NET Aspire HostApp project, see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire.AppHost/MauiAspire.AppHost.csproj#L19) in this sample project.
* Add your .NET MAUI app to the .NET Aspire AppHost [**Program.cs**](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire.AppHost/Program.cs) and add any references to services that you want to consume in your .NET MAUI app. For example:
  ```csharp
  // This was probably already there
  var builder = DistributedApplication.CreateBuilder(args);

  // This is a service you want to call from your .NET MAUI app and/or other projects
  var webapi = builder.AddProject<Projects.SampleWebApi>("webapi");

  // This registers your .NET MAUI app and adds a reference to the Web API you want to call
  builder.AddProject<Projects.MauiAspire>("mauiapp")
      .WithReference(webapi);

  await builder.WithDevTunnels();

  // This was probably already there
  builder.Build().Run();
  ```
* Add .NET MAUI ServiceDefaults project to the solution
  * This is a new template in the artifacts above, or take it from: https://github.com/jfversluis/MauiAspire/tree/main/MauiApp.ServiceDefaults
* Add a [reference](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiAspire.csproj#L77) to the .NET MAUI ServiceDefaults project in your .NET MAUI app
* Go into your [**MauiProgram.cs**](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiProgram.cs#L19) in your .NET MAUI project and add `builder.AddServiceDefaults();` also make sure to add `using Microsoft.Extensions.Hosting;` in the top of your file.
* Start registering your services/HttpClient instances, for example (of course make sure that your service discovery endpoints match, in this case `webapi` must match how your Web API is registered in the .NET Aspire AppHost project), see [here](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire/MauiProgram.cs#L21-L26) in this sample project:
  ```csharp
  builder.Services.AddHttpClient<WeatherApiClient>(client =>
  {
      // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
      // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
      client.BaseAddress = new("https+http://webapi");
  });
  ```
* Go into the .NET Aspire AppHost project's **launchSettings.json** file and note the port number for [`DOTNET_DASHBOARD_OTLP_ENDPOINT_URL`](https://github.com/jfversluis/MauiAspire/blob/main/MauiAspire.AppHost/Properties/launchSettings.json#L13) (for example 21104)
* Go into the Web API [**launchSettings.json**](https://github.com/jfversluis/MauiAspire/blob/main/SampleWebApi/Properties/launchSettings.json#L17) file and note the port number of the application. (for example 7291)
  * In this example I assume we're only using the https one. The http endpoint should work as well, you will need to note this port too and add it to the Dev Tunnel command a little later.
* Make sure Dev Tunnels is installed: https://learn.microsoft.com/azure/developer/dev-tunnels/get-started and you are logged in
* Consume your registered service/HttpClient in your .NET MAUI app
* Set the AppHost project as the startup project, click the run button and all should light up and you should see all data in the .NET Aspire dashboard!
