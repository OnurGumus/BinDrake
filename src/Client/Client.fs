module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.Validation.Core
open Shared
open System
open Feliz.Bulma
open Feliz
open Feliz.Bulma.Bulma

type Calculation =
    | NotCalculated
    | Calculating
    | Calculated of CalcResult

type RowItem =
    {
        Width: int
        Height: int
        Length: int
        Color: string
        Quantity: int
        Stackable: bool
    }

type ContainerItem =
    {
        Width: int
        Height: int
        Length: int
    }

type Model =
    {
        Calculation: Calculation
        Container: Container option
        ContainerItem: ContainerItem option
        RowItems: (RowItem option * string) list
        TotalVolume: int option
    }

type Msg =
    | ResultLoaded of CalcResult
    | CalculateRequested
    | AddRow
    | RemoveItem of string
    | RowUpdated of string * RowItem option
    | ContainerUpdated of ContainerItem option

module Server =

    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api: ICounterApi =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.buildProxy<ICounterApi>

let run = Server.api.run

let runCmd container items =
    Cmd.OfAsync.perform (fun _ -> run container items 1000. 0.9) () ResultLoaded

let newRowItem () = (None, Guid.NewGuid().ToString())

let numericCheck (t: Validator<_>) typef min max name data =
    t.Test name data
    |> t.NotBlank "cannot be blank"
    |> t.To typef "must be a number"
    |> t.Gt min "must be greater than is {min}"
    |> t.Lt max "must be less than is {max}"
    |> t.End

let init () =
    CanvasRenderer.init ()

    let colors =
        [
            "green"
            "blue"
            "red"
            "pink"
            "yellow"
            "aqua"
            "orange"
            "white"
            "purple"
            "lime"
        ]

    let boxes: ItemPut list =
        [
            for i = 0 to 9 do
                {
                    Coord =
                        {
                            X = i * 10
                            Y = Math.Abs(5 - i) * 10
                            Z = Math.Abs(5 - i) * 10
                        }
                    Item =
                        {
                            Dim = { Width = 10; Height = 10; Length = 10 }
                            Tag = colors.[i]
                            Id = i.ToString()
                            NoTop = false
                        }
                }
            for i = 0 to 9 do
                {
                    Coord =
                        {
                            X = (i * 10)
                            Y = Math.Abs(5 - i) * 10
                            Z = 90 - Math.Abs(5 - i) * 10
                        }
                    Item =
                        {
                            Dim = { Width = 10; Height = 10; Length = 10 }
                            Tag = colors.[i]
                            Id = i.ToString()
                            NoTop = false
                        }
                }
        ]

    let container: Container =
        {
            Dim =
                {
                    Width = 100
                    Height = 100
                    Length = 100
                }
            Coord = { X = 0; Y = 0; Z = 0 }
        }

    CanvasRenderer.renderResult container boxes true
    {
        Calculation = NotCalculated
        Container = None
        ContainerItem = None
        RowItems = [ newRowItem () ]
        TotalVolume = None
    },
    Cmd.none

let cols =
    [
        "Height"
        "Width"
        "Length"
        "Quantity"
        "Stackable"
        "Color"
        ""
        ""
    ]

let validateTreshold v =
    single
    <| fun t ->
        t.TestOne v
        |> t.NotBlank "cannot be blank"
        |> t.To float "must be a number"
        |> t.Gt 0. "must be greater than {min}"
        |> t.Lt 100_000. "must be less than {max}"
        |> t.End

let update (msg: Msg) model =
    let model, cmd =
        match msg with
        | AddRow ->
            { model with
                RowItems = model.RowItems @ [ newRowItem () ]
            },
            Cmd.none
        | RemoveItem key ->
            { model with
                RowItems =
                    model.RowItems
                    |> List.filter (fun (r, k) -> k <> key)
            },
            Cmd.none
        | RowUpdated (key, row) ->
            { model with
                RowItems =
                    model.RowItems
                    |> List.map (fun ((_, oldKey) as old) -> if oldKey = key then (row, key) else old)
            },
            Cmd.none
        | ContainerUpdated c -> { model with ContainerItem = c }, Cmd.none

    let totalVolume =
        match model.RowItems with
        | rowItems when rowItems |> List.forall (fun x -> (fst x).IsSome) ->
            let rowItems =
                rowItems |> List.map (fun x -> (x |> fst).Value)

            let vol =
                rowItems
                |> List.sumBy (fun x -> x.Width * x.Height * x.Length * (x.Quantity))

            Some vol
        | _ -> None

    { model with TotalVolume = totalVolume }, cmd


type RowProp =
    {
        RowUpdated: RowItem option -> unit
        AddRow: (unit -> unit) option
        Remove: (unit -> unit) option
        Key: string
    }

type ContainerProp =
    {
        ContainerUpdated: ContainerItem option -> unit
    }

module Container =
    open Feliz.UseElmish

    type FormData =
        {
            Width: string
            Height: string
            Length: string
        }

    type Model =
        {
            ContainerItem: Result<ContainerItem, Map<string, string list>> option
            FormData: FormData
        }

    type Msg =
        | WidthChanged of string
        | HeightChanged of string
        | LengthChanged of string


    let init () =
        {
            ContainerItem = None
            FormData = { Width = ""; Height = ""; Length = "" }
        },
        Cmd.none

    let validate (formData: FormData) =
        all
        <| fun t ->
            let floatCheck = numericCheck t int 0 100_00
            let intCheck = numericCheck t int 0 100_00
            {
                Width = floatCheck "width" formData.Width
                Height = floatCheck "height" formData.Height
                Length = floatCheck "length" formData.Length
            }: ContainerItem

    let update containerUpdated msg (state: Model) =
        let formData = state.FormData

        let formData =
            match msg with
            | WidthChanged s -> { formData with Width = s }
            | HeightChanged s -> { formData with Height = s }
            | LengthChanged s -> { formData with Length = s }

        let r = validate formData

        let cmd =
            match r with
            | Ok _ when Some r = state.ContainerItem -> Cmd.none
            | Ok r -> Cmd.ofSub (fun _ -> containerUpdated (Some r))
            | Error _ -> Cmd.ofSub (fun _ -> containerUpdated None)

        { state with
            ContainerItem = Some r
            FormData = formData
        },
        cmd

    let view =
        React.functionComponent
            ((fun (props: ContainerProp) ->
                let model, dispatch =
                    React.useElmish (init, update props.ContainerUpdated, [||])

                let dispatch' col v =
                    match col with
                    | "Height" -> HeightChanged v
                    | "Width" -> WidthChanged v
                    | "Length" -> LengthChanged v
                    | other -> failwith other
                    |> dispatch

                Html.div
                    [
                        let cols = [ "Width"; "Height"; "Length" ]
                        prop.className "table"
                        prop.children [
                            Html.div [
                                prop.className "tr"
                                prop.children
                                    [
                                        for col in cols do
                                            Html.div [
                                                prop.classes [ "td"; "th" ]
                                                prop.children
                                                    [
                                                        Bulma.label [
                                                            control.isSmall
                                                            prop.text col
                                                        ]
                                                    ]
                                            ]
                                    ]
                            ]
                            Html.div [
                                prop.className "tr"
                                prop.children
                                    [
                                        for col in cols do
                                            control.div [
                                                prop.className "td"
                                                prop.children
                                                    [
                                                        input.number [
                                                            prop.maxLength 5
                                                            prop.max 100000
                                                            input.isSmall
                                                            prop.placeholder col
                                                        ]
                                                    ]
                                            ]
                                    ]
                            ]
                        ]
                    ]))

module Row =
    open Feliz.UseElmish

    type FormData =
        {
            Width: string
            Height: string
            Length: string
            Quantity: string
            Color: string
            Stackable: bool
        }

    type Model =
        {
            RowItem: Result<RowItem, Map<string, string list>> option
            FormData: FormData
        }

    type Msg =
        | WidthChanged of string
        | HeightChanged of string
        | LengthChanged of string
        | StackableChanged of bool
        | QuantityChanged of string

    let r = Random()

    let init () =
        {
            RowItem = None
            FormData =
                {
                    Width = ""
                    Height = ""
                    Length = ""
                    Color = sprintf "rgb(%i,%i,%i)" (r.Next(256)) (r.Next(256)) (r.Next(256))
                    Stackable = true
                    Quantity = ""
                }
        },
        Cmd.none

    let validate (formData: FormData) =
        all
        <| fun t ->
            let floatCheck = numericCheck t int 0 100_00
            let intCheck = numericCheck t int 0 100_00
            {
                Width = floatCheck "width" formData.Width
                Height = floatCheck "height" formData.Height
                Length = floatCheck "length" formData.Length
                Quantity = intCheck "quantity" formData.Quantity
                Stackable = formData.Stackable
                Color = formData.Color
            }: RowItem

    let update rowUpdated msg (state: Model) =
        let formData = state.FormData

        let formData =
            match msg with
            | WidthChanged s -> { formData with Width = s }
            | HeightChanged s -> { formData with Height = s }
            | LengthChanged s -> { formData with Length = s }
            | QuantityChanged s -> { formData with Quantity = s }
            | StackableChanged s -> { formData with Stackable = s }

        let r = validate formData

        let cmd =
            match r with
            | Ok _ when Some r = state.RowItem -> Cmd.none
            | Ok r -> Cmd.ofSub (fun _ -> rowUpdated (Some r))
            | Error _ -> Cmd.ofSub (fun _ -> rowUpdated None)


        { state with
            RowItem = Some r
            FormData = formData
        },
        cmd

    let view =
        React.functionComponent
            ((fun (props: RowProp) ->
                let model, dispatch =
                    React.useElmish (init, update props.RowUpdated, [||])

                let removeButton =
                    Bulma.button.button [
                        button.isSmall
                        color.isDanger
                        prop.text "Remove"
                        match props.Remove with
                        | Some remove -> prop.onClick (fun _ -> remove ())
                        | _ -> prop.style [ style.visibility.hidden ]
                    ]

                let addButton =
                    Bulma.button.button [
                        button.isSmall
                        color.isPrimary
                        prop.text "Add"
                        match props.AddRow with
                        | Some addRow -> prop.onClick (fun _ -> addRow ())
                        | _ -> prop.style [ style.visibility.hidden ]
                    ]

                let dispatch' col v =
                    match col with
                    | "Height" -> HeightChanged v
                    | "Width" -> WidthChanged v
                    | "Quantity" -> QuantityChanged v
                    | "Stackable" -> StackableChanged(Boolean.Parse v)
                    | "Length" -> LengthChanged v
                    | other -> failwith other
                    |> dispatch

                Html.div [
                    prop.className "tr"
                    prop.children
                        [
                            for i, col in cols |> List.indexed do
                                control.div [
                                    prop.className "td"
                                    prop.children
                                        [
                                            if i < cols.Length - 2 then
                                                match col with
                                                | "Stackable" ->
                                                    input.checkbox [
                                                        input.isSmall
                                                        prop.onChange (fun (e: Browser.Types.Event) ->
                                                            dispatch' col e.Value)
                                                    ]
                                                | "Color" ->
                                                    input.text [
                                                        input.isSmall
                                                        prop.readOnly true
                                                        prop.style
                                                            [
                                                                style.backgroundColor model.FormData.Color
                                                            ]
                                                    ]
                                                | _ ->
                                                    input.number [
                                                        prop.maxLength 5
                                                        prop.max 100000
                                                        input.isSmall
                                                        prop.placeholder col
                                                        prop.onChange (fun (e: Browser.Types.Event) ->
                                                            dispatch' col e.Value)
                                                    ]
                                            else if i = cols.Length - 2 then
                                                removeButton
                                            else
                                                addButton
                                        ]
                                ]
                        ]
                ]

             ),
             (fun props -> props.Key))

let view (model: Model) dispatch =
    printf "%A" model

    let rowItems =
        [
            for i, (_, key) in model.RowItems |> List.indexed do
                let addRow =
                    if i = model.RowItems.Length - 1 then Some(fun () -> dispatch AddRow) else None

                let remove =
                    if model.RowItems.Length > 1 then Some(fun _ -> dispatch (RemoveItem key)) else None

                Row.view
                    {
                        RowUpdated = fun r -> dispatch (RowUpdated(key, r))
                        AddRow = addRow
                        Key = key
                        Remove = remove
                    }
        ]

    let content =
        field.div [
            Html.p [
                prop.style [ style.fontWeight.bold ]
                prop.text "All dimensions are between 1 and 10000 and quantity must be an integer."
            ]
            br []
            Bulma.label [
                prop.text "Enter CONTAINER dimensions:"
                control.isSmall
            ]
            Container.view
                {
                    ContainerUpdated = fun r -> dispatch (ContainerUpdated(r))
                }

            Bulma.label [
                prop.text "Enter ITEM dimensions:"
                control.isSmall
            ]
            Html.div [
                prop.className "table"
                prop.children [
                    Html.div [
                        prop.className "tr"
                        prop.children
                            [
                                for col in cols do
                                    Html.div [
                                        prop.classes [ "td"; "th" ]
                                        prop.children
                                            [
                                                Bulma.label [
                                                    control.isSmall
                                                    prop.text col
                                                ]
                                            ]
                                    ]
                            ]
                    ]
                    rowItems |> ofList
                ]
            ]

            let line (title: string) (v: int option) =
                React.fragment [
                    br []
                    Bulma.label title
                    control.div
                        [
                            Html.output [
                                if title.StartsWith "Chargable" && v.IsSome
                                then prop.className "output"
                                prop.text
                                    (v
                                     |> Option.map (string)
                                     |> Option.defaultValue "Please complete the form.")
                            ]
                        ]
                ]

            [
                let items = [ ("Total Volume:", model.TotalVolume) ]

                for t, v in items do
                    line t v
            ]
            |> ofList
            let isinvalid = (model.ContainerItem.IsNone && model.TotalVolume.IsNone)
            Bulma.button.button[
                prop.disabled isinvalid
                color.isPrimary
                prop.text (if isinvalid then "First fill the form correctly" else"Calculate")
                prop.onClick(fun _ -> dispatch CalculateRequested)

            ]
        ]

    Bulma.container
        [
            prop.children
                [
                    Bulma.columns
                        [
                            prop.children
                                [
                                    Bulma.column
                                        [
                                            prop.children
                                                [
                                                    Bulma.panel [
                                                        spacing.mt6
                                                        prop.children [
                                                            Bulma.panelHeading
                                                                [
                                                                    Html.span [
                                                                        prop.style [ style.color.white ]
                                                                        prop.text
                                                                            "Air freight volumetric (chargeable weight) calculator"
                                                                    ]
                                                                ]
                                                            Bulma.panelBlock.div [ content ]
                                                        ]
                                                    ]
                                                ]
                                        ]
                                ]
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
