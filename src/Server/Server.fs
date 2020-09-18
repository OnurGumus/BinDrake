open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open FSharp.Control.Tasks.V2
open Giraffe
open Shared
open Serilog
open Serilog.Sinks.SystemConsole
open Serilog.Sinks.File
open Microsoft.ApplicationInsights.Extensibility
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

#if DEBUG
let publicPath = Path.GetFullPath "../Client/public"
#else
let publicPath = Path.GetFullPath "./clientFiles"
#endif
let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let counterApi = {
    initialCounter = fun () -> async { return { Value = 42 } }
    run = fun calculationMode container items t alpha -> async {return  BinPacker.run  container calculationMode items t alpha }
}

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue counterApi
    |> Remoting.buildHttpHandler


let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseDeveloperExceptionPage()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

Log.Logger <-
          LoggerConfiguration().MinimumLevel.Information()
            .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
            .WriteTo.File("log.txt", rollingInterval= RollingInterval.Day)
            .Destructure.FSharpTypes()
            .WriteTo.Console()
            .CreateLogger();
WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
   // .UseKestrel(fun x-> x.Limits.KeepAliveTimeout <- System.TimeSpan.FromMinutes(100.))
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    .Run()
