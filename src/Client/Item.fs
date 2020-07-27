module ItemAdd
open Shared
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Thoth.Json
open Fable.Core.JsInterop
open Shared
open System
type Model = {
    Items : Item list
    ItemWidth : int option
    ItemHeight : int option
    ItemLength : int option
    Quantity :int option
    Id : string
}
type Msg =
    | WidthChanged of string
    | HeightChanged of string
    | LengthChanged of string
    | QuantityChanged of string
    | RemoveItem

let init id : Model * Cmd<Msg> =
    let initialModel = {
        Id = id
        Items = []
        ItemWidth = None
        ItemHeight = None
        ItemLength = None
        Quantity = None
    }
    initialModel, Cmd.none

let (|Int|_|) (s:string) =
    match Int32.TryParse s with
    | true, v when v > 0 && v < 10000 -> Some v
    | _ -> None

let r = Random()
let fillContainer (model:Model) =
    match model.ItemLength, model.ItemHeight, model.ItemWidth , model.Quantity with
    | Some l, Some h, Some w , Some q->
        let tag = sprintf "rgb(%i,%i,%i)" (r.Next(256)) (r.Next(256)) (r.Next(256))
        { model with Items  = [
                                for i = 1 to q do
                                        { Dim = { Width = w; Height = h; Length =l}; Id = model.Id + i.ToString(); Tag= tag }
            ]
        }
    | _ -> { model with Items = [] }

let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match model, msg with
    |_, WidthChanged (Int i) ->
        {model with ItemWidth = Some i} |> fillContainer, Cmd.none
     |_, WidthChanged _ ->
        {model with ItemWidth = None} |> fillContainer, Cmd.none
    |_, HeightChanged (Int i) ->
        {model with ItemHeight = Some i} |> fillContainer, Cmd.none
    |_, HeightChanged _ ->
        {model with ItemHeight = None} |> fillContainer, Cmd.none
    |_, LengthChanged (Int i) ->
        {model with ItemLength = Some i} |> fillContainer, Cmd.none
    |_, LengthChanged _ ->
        {model with ItemLength = None} |> fillContainer, Cmd.none
    |_, QuantityChanged (Int i) ->
        {model with Quantity = Some i} |> fillContainer, Cmd.none
     |_, QuantityChanged _ ->
        {model with Quantity = None } |> fillContainer, Cmd.none
    | _ ->
        model,Cmd.none

open Feliz

let view (model : Model) (dispatch : Msg -> unit) =
    Html.div[

        Html.div[
                prop.children[
                    Html.label[
                        prop.htmlFor "item-width"
                        prop.text "Width"
                    ]
                    Html.input[
                        prop.name "item-width"
                        prop.onInput(fun ev -> ev.target?value |> string |> WidthChanged |> dispatch )
                    ]
                    Html.label[
                        prop.htmlFor "item-height"
                        prop.text "Height"
                    ]
                    Html.input[
                        prop.name "item-height"
                        prop.onInput(fun ev -> ev.target?value |> string |> HeightChanged |> dispatch )
                    ]
                    Html.label[
                        prop.htmlFor "item-length"
                        prop.text "Length"
                    ]
                    Html.input[
                        prop.name "item-length"
                        prop.onInput(fun ev -> ev.target?value |> string |> LengthChanged |> dispatch )
                    ]
                    Html.label[
                        prop.htmlFor "item-quantity"
                        prop.text "Quantity"
                    ]
                    Html.input[
                        prop.name "item-quantity"
                        prop.onInput(fun ev -> ev.target?value |> string |> QuantityChanged |> dispatch )
                    ]
                ]
        ]
        Html.button[ prop.text "remove"; prop.onClick(fun _ -> dispatch RemoveItem)]
    ]