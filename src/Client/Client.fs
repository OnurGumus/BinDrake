module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Thoth.Json

open Shared

type Model =
    | NotCalculated
    | Calculating
    | Calculated of CalcResult

type Msg =
    | ResultLoaded of CalcResult
    | CalculateRequested

module Server =

    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : ICounterApi =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ICounterApi>

let run  = Server.api.run
let runCmd =
        Cmd.OfAsync.perform
            (fun _ -> run CanvasRenderer.containers CanvasRenderer.items 1000. 0.9)() ResultLoaded

let init () : Model * Cmd<Msg> =
    let initialModel = NotCalculated
    initialModel, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel, msg with
    |_ , ResultLoaded res ->
        CanvasRenderer.renderResult res ;
        (Calculated res) , Cmd.none
    |_ , CalculateRequested -> Calculating, runCmd

    | _, _ -> currentModel, Cmd.none


let view (model : Model) (dispatch : Msg -> unit) =

    button
        [
            OnClick (fun _ -> CalculateRequested |> dispatch )
            Disabled (match model with | Calculating _ -> true | _ -> false)
        ]
        [ str "Calculate"]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
