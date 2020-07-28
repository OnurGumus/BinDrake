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
    ItemAddModels : ItemAdd.Model list
}
type Msg =
    | ResultLoaded of CalcResult
    | ContainerWidthChanged of string
    | ContainerHeightChanged of string
    | ContainerLengthChanged of string
    | CalculateRequested
    | ItemAddMsg of (string*ItemAdd.Msg)
    | AddItem

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
    CanvasRenderer.init()
    let initialModel = {
        Calculation = NotCalculated
        Container = None
        ContainerWidth = None
        ContainerHeight = None
        ContainerLength = None
        ItemAddModels = []
    }
    initialModel, Cmd.none

let inline replace1 list a b = list |> List.map (fun x -> if x = a then b else x)

let (|Int|_|) (s:string) =
    match Int32.TryParse s with
    | true, v when v > 0 && v <3000-> Some v
    | _ -> None


let org = { X = 0; Y= 0; Z = 0;}
let fillContainer (model:Model) =
    match model.ContainerLength, model.ContainerHeight, model.ContainerWidth with
    | Some l, Some h, Some w ->
        { model with Container = Some { Dim = { Width = w; Height = h; Length =l}; Coord=org }}
    | _ ->{ model with Container = None }

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match model, msg with
    |_, ContainerWidthChanged (Int i) ->
        {model with ContainerWidth = Some i} |> fillContainer, Cmd.none
    |_, ContainerWidthChanged _ ->
        {model with ContainerWidth = None} |> fillContainer, Cmd.none
    |_, ContainerHeightChanged (Int i) ->
        {model with ContainerHeight = Some i} |> fillContainer, Cmd.none
    |_, ContainerHeightChanged _ ->
        {model with ContainerHeight = None} |> fillContainer , Cmd.none
    |_, ContainerLengthChanged (Int i) ->
        {model with ContainerLength = Some i} |> fillContainer, Cmd.none
     |_, ContainerLengthChanged _ ->
        {model with ContainerLength = None} |> fillContainer , Cmd.none
    | _ , AddItem ->
        let id = Guid.NewGuid().ToString()
        let subm , subc = ItemAdd.init(id)
        {model with ItemAddModels = (model.ItemAddModels@[subm])}, Cmd.map(fun m -> ItemAddMsg(id,m) ) subc
    |_ , ItemAddMsg (id , ItemAdd.RemoveItem ) ->
            let newd = model.ItemAddModels |> List.filter(fun i -> i.Id <> id)
            {model with ItemAddModels = (newd)} ,Cmd.none
    |_ , ItemAddMsg (id , msg ) ->
            let existing = model.ItemAddModels |> List.find (fun i -> i.Id = id)
            let subm , subc  = ItemAdd.update msg existing
            let newd = replace1 model.ItemAddModels  existing subm
            {model with ItemAddModels = (newd)} ,Cmd.map(fun m -> ItemAddMsg(id,m) ) subc

    |_ , ResultLoaded res ->
        CanvasRenderer.renderResult res ;
        {model with Calculation = (Calculated res)} , Cmd.none
    |{Container = Some c}, CalculateRequested ->
        let items = model.ItemAddModels |> List.collect(fun i -> i.Items)
        {model with Calculation = Calculating},
            runCmd c items

    | _, _ -> model, Cmd.none

open Feliz
open Fable.Core.JsInterop
let view (model : Model) (dispatch : Msg -> unit) =
    Html.div[
        prop.className "card"
        prop.children[
            Html.div[
                prop.className "card-body"
                prop.children[
                    Html.h4[ prop.className "card-title"; prop.text "Container dimensions" ]
                    Html.h5[ prop.text "Enter integers between 1 and 3000"]
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
                    let calcDisabled =
                        (match model.Calculation, model.Container, model.ItemAddModels with
                                | _ ,_ ,items when items |> List.exists(fun x-> x.Items.Length = 0) -> true
                                | _ ,_ ,[] -> true
                                | _, None,_ -> true
                                | Calculating, _,_ -> true
                                | _ -> false)
                    Html.button [
                        prop.className "btn-small"
                        prop.onClick (fun _ -> CalculateRequested |> dispatch )
                        prop.style[style.opacity 1.]
                        prop.disabled calcDisabled
                        if calcDisabled then
                            prop.custom("popover-top","Fill all the data")
                        prop.text "Calculate"
                    ]
                    Html.button [
                        prop.onClick (fun _ -> AddItem |> dispatch )
                        prop.disabled
                            (match model.Calculation with
                            | Calculating -> true
                            | _ -> false)
                        prop.text "Add item"
                        prop.className "btn-small"
                    ]
                ]
            ]
            for item in model.ItemAddModels do
                ItemAdd.view item ((fun m -> ItemAddMsg(item.Id,m) ) >> dispatch)
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
