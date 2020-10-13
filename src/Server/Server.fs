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
open Thoth.Json.Net
open System
open System.Diagnostics

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x
let sw (swReal:Stopwatch) =
    { new IStopwatch with
        member this.ElapsedMilliseconds = swReal.ElapsedMilliseconds }
let logger : Shared.ILogger =
    { new Shared.ILogger with
        member this.LogError e = Serilog.Log.Error(e,"error")
        member this.Log str arr = Serilog.Log.Information(str, arr)
                }
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

    run = fun calcs container items t alpha -> async {return
        BinPacker.run (sw (Stopwatch.StartNew())) logger container  calcs.ContainerMode calcs.CalculationMode items t alpha }
    saveModel = fun model ->
        async{
            let guid = Guid.NewGuid()
            let path = "saved_data_" + guid.ToString() + ".txt"
            let data = Encode.Auto.toString(4, model)
            do! File.WriteAllTextAsync(path,data) |> Async.AwaitTask
            return guid
        }
    loadModel = fun guid ->
        async{
            let path = "saved_data_" + guid.ToString() + ".txt"
            let! datas = File.ReadAllTextAsync(path) |> Async.AwaitTask
            let data = Decode.Auto.fromString<Shared.ClientModel.Model>(datas)
            match data with
            | Ok data -> return data
            | _ -> return failwith "Error"
        }
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
