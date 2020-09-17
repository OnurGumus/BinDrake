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
    ItemWeight : int option
    Quantity :int option
    Color : string
    Id : string
    NoTop : bool
    KeepTop : bool
}
type Msg =
    | WidthChanged of string
    | HeightChanged of string
    | LengthChanged of string
    | WeightChanged of string
    | QuantityChanged of string
    | NoTopChanged of bool
    | KeepTopCanged of bool
    | RemoveItem
let r = Random()

let init id : Model * Cmd<Msg> =
    printf "init"
    let initialModel = {
        Id = id
        Items = []
        ItemWidth = None
        ItemHeight = None
        ItemLength = None
        ItemWeight = None
        Quantity = None
        NoTop = false
        KeepTop = false
        Color =  sprintf "rgb(%i,%i,%i)" (r.Next(256)) (r.Next(256)) (r.Next(256))
    }
    initialModel, Cmd.none

let (|Int|_|) (s:string) =
    match Int32.TryParse s with
    | true, v when v > 0 && v < 10000 -> Some v
    | _ -> None

let fillContainer (model:Model) =
    match model.ItemLength, model.ItemHeight, model.ItemWidth , model.ItemWeight, model.Quantity with
    | Some l, Some h, Some w , Some wt, Some q->

        { model with Items  = [ for i = 1 to q do  { Dim = { Width = w; Height = h; Length =l}; Weight = wt; Id = model.Id + i.ToString(); Tag= model.Color; NoTop = model.NoTop; KeepTop = model.KeepTop }]}
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
    |_, WeightChanged (Int i) ->
        {model with ItemWeight = Some i} |> fillContainer, Cmd.none
    |_, WeightChanged _ ->
        {model with ItemWeight = None} |> fillContainer, Cmd.none
    |_, QuantityChanged (Int i) ->
        {model with Quantity = Some i} |> fillContainer, Cmd.none
    |_, QuantityChanged _ ->
        {model with Quantity = None } |> fillContainer, Cmd.none
    |_ , NoTopChanged b ->
         {model with NoTop = b } |> fillContainer, Cmd.none
    |_ , KeepTopCanged b ->
         {model with KeepTop = b } |> fillContainer, Cmd.none
    | _ ->
        model,Cmd.none

open Feliz

let view (model : Model) (dispatch : Msg -> unit) =
    Html.div[
        prop.className "card"
        prop.children[
            Html.div[
                    prop.className "card-body"
                    prop.children[
                        Html.div[
                        Html.h5[ prop.text "Item dimensions (between 1 and 10000)"; prop.style[style.display.inlineFlex]]
                        Html.button[ prop.className "btn-small"; prop.style[style.marginLeft 110; style.marginBottom 10];
                            prop.text "remove"; prop.onClick(fun _ -> dispatch RemoveItem)]]
                        Html.input[
                            prop.name "item-width"
                            prop.placeholder "width"
                            prop.onInput(fun ev -> ev.target?value |> string |> WidthChanged |> dispatch )
                        ]

                        Html.input[
                            prop.placeholder "height"
                            prop.name "item-height"
                            prop.onInput(fun ev -> ev.target?value |> string |> HeightChanged |> dispatch )
                        ]

                        Html.input[
                            prop.name "item-length"
                            prop.placeholder "length"
                            prop.onInput(fun ev -> ev.target?value |> string |> LengthChanged |> dispatch )
                        ]

                        Html.input[
                            prop.name "item-quantity"
                            prop.placeholder "quan."
                            prop.onInput(fun ev -> ev.target?value |> string |> QuantityChanged |> dispatch )
                        ]
                        Html.label[
                            prop.htmlFor "keep-ntop"
                            prop.text "Keep Top:"
                        ]
                        Html.input[
                            prop.name "keep-top"
                            prop.type' "checkbox"
                            prop.onCheckedChange(fun ev -> ev |> KeepTopCanged |> dispatch )
                        ]
                        Html.label[
                            prop.htmlFor "item-notop"
                            prop.text "Nothing on Top:"
                        ]
                        Html.input[
                            prop.name "item-notop"
                            prop.type' "checkbox"
                            prop.onCheckedChange(fun ev -> ev |> NoTopChanged |> dispatch )
                        ]

                        Html.input[
                            prop.style[style.width 500]
                            prop.name "item-color"
                            prop.placeholder "Color"
                            prop.readOnly true
                            prop.style [ style.backgroundColor (model.Color)]
                        ]
                    ]
            ]

        ]
    ]