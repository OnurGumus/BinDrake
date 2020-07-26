module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Thoth.Json

open Shared
open System

type Calculation =
    | NotCalculated
    | Calculating
    | Calculated of CalcResult

type Model = {
    Calculation : Calculation
    Container : Container option
    ContainerWidth : int option
    ContainerHeight : int option
    ContainerLength : int option
}
type Msg =
    | ResultLoaded of CalcResult
    | ContainerWidthChanged of string
    | ContainerHeightChanged of string
    | ContainerLengthChanged of string
    | CalculateRequested

module Server =

    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : ICounterApi =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ICounterApi>

let run  = Server.api.run
let runCmd container items  =
        Cmd.OfAsync.perform
            (fun _ -> run container items 1000. 0.9)() ResultLoaded

let init () : Model * Cmd<Msg> =
    let initialModel = {
        Calculation = NotCalculated
        Container = None
        ContainerWidth = None
        ContainerHeight = None
        ContainerLength = None
    }
    initialModel, Cmd.none

let (|Int|_|) (s:string) =
    match Int32.TryParse s with
    | true, v when v > 0 -> Some v
    | _ -> None
let org = { X = 0; Y= 0; Z = 0;}
let fillContainer (model:Model) =
    match model.ContainerLength, model.ContainerHeight, model.ContainerWidth with
    | Some l, Some h, Some w ->
        { model with Container = Some { Dim = { Width = w; Height = h; Length =l}; Coord=org }}
    | _ -> model
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel, msg with
    |_, ContainerWidthChanged (Int i) ->
        {currentModel with ContainerWidth = Some i} |> fillContainer, Cmd.none
    |_, ContainerHeightChanged (Int i) ->
        {currentModel with ContainerHeight = Some i} |> fillContainer, Cmd.none
    |_, ContainerLengthChanged (Int i) ->
        {currentModel with ContainerLength = Some i} |> fillContainer, Cmd.none

    |_ , ResultLoaded res ->
        CanvasRenderer.renderResult res ;
        {currentModel with Calculation = (Calculated res)} , Cmd.none
    |{Container = Some c}, CalculateRequested ->
        {currentModel with Calculation = Calculating},
            runCmd c CanvasRenderer.items

    | _, _ -> currentModel, Cmd.none

open Feliz
open Fable.Core.JsInterop
let view (model : Model) (dispatch : Msg -> unit) =
    Html.div[
        prop.children[
            Html.label[
                prop.htmlFor "container-width"
                prop.text "Width"
            ]
            Html.input[
                prop.name "container-width"
                prop.onInput(fun ev -> ev.target?value |> string |> ContainerWidthChanged |> dispatch )
            ]
            Html.label[
                prop.htmlFor "container-height"
                prop.text "Height"
            ]
            Html.input[
                prop.name "container-height"
                prop.onInput(fun ev -> ev.target?value |> string |> ContainerHeightChanged |> dispatch )
            ]
            Html.label[
                prop.htmlFor "container-length"
                prop.text "Length"
            ]
            Html.input[
                prop.name "container-length"
                prop.onInput(fun ev -> ev.target?value |> string |> ContainerLengthChanged |> dispatch )
            ]
            Html.button [
                prop.onClick (fun _ -> CalculateRequested |> dispatch )
                prop.disabled
                    (match model.Calculation, model.Container with
                    | _, None -> true
                    | Calculating, _ -> true
                    | _ -> false)
                prop.text "Calculate"
            ]
        ]
    ]

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
