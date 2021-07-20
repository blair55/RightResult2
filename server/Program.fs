module Program

open System
open System.IO
open Giraffe
open Shared
open Server
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder

let webApi : HttpHandler =
    Remoting.createApi ()
    |> Remoting.fromContext (fun (ctx: HttpContext) -> ctx.GetService<ServerApi>().Build())
    |> Remoting.withRouteBuilder routerPaths
    |> Remoting.buildHttpHandler

let webApp : HttpHandler =
    choose [ webApi
            //  GET >=> text "Welcome to full stack F#"
           ]

let configureApp (app: IApplicationBuilder) = app.UseFileServer().UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    services
        .AddSingleton<ServerApi>()
        .AddLogging()
        .AddGiraffe()
    |> ignore

type LambdaEntryPoint() =

    inherit Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction()

    override this.Init(builder: IWebHostBuilder) =

        Env
            .configureHost(builder)
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
        |> ignore

[<EntryPoint>]
let main _ =
    Env
        .configureHost(WebHostBuilder())
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    0
