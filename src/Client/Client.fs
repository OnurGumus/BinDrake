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
open Browser.Types

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
        "Quant."
        "Stack"
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
        | ResultLoaded c ->
            { model with
                Calculation = Calculated c
            },
            Cmd.ofSub (fun _ -> CanvasRenderer.renderResult c.Container c.ItemsPut false)

        | ContainerUpdated c -> { model with ContainerItem = c }, Cmd.none
        | CalculateRequested _ ->
            let c = model.ContainerItem.Value

            let container: Container =
                {
                    Coord = { X = 0; Y = 0; Z = 0 }
                    Dim =
                        {
                            Width = c.Width
                            Height = c.Height
                            Length = c.Length
                        }
                }

            let items: list<Item> =
                [
                    for rowItem, key in model.RowItems do
                        let r = rowItem.Value
                        for i = 1 to r.Quantity do
                            yield {
                                      Tag = r.Color
                                      Id = key + i.ToString()
                                      NoTop = not (r.Stackable)
                                      Dim =
                                          {
                                              Width = r.Width
                                              Height = r.Height
                                              Length = r.Length
                                          }
                                  }
                ]

            { model with Calculation = Calculating }, runCmd container items

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
        Disabled: bool
    }

type ContainerProp =
    {
        ContainerUpdated: ContainerItem option -> unit
        Disabled: bool
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
                                                            prop.readOnly props.Disabled
                                                            prop.maxLength 5
                                                            prop.max 100000
                                                            input.isSmall
                                                            prop.placeholder col
                                                            prop.onChange (fun (e: Event) -> dispatch' col e.Value)
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
                        prop.disabled props.Disabled
                        color.isDanger
                        prop.text "X"
                        match props.Remove with
                        | Some remove -> prop.onClick (fun _ -> remove ())
                        | _ -> prop.style [ style.visibility.hidden ]
                    ]

                let addButton =
                    Bulma.button.button [
                        button.isSmall
                        prop.disabled props.Disabled
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
                    | "Quant." -> QuantityChanged v
                    | "Stack" -> StackableChanged(Boolean.Parse v)
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
                                                | "Stack" ->
                                                    input.checkbox [
                                                        input.isSmall
                                                        prop.readOnly props.Disabled
                                                        prop.defaultChecked true
                                                        prop.onCheckedChange (fun e -> dispatch' "Stack" (e.ToString()))
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
                                                        prop.readOnly props.Disabled
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

open Fable.Core
open Browser.Dom
open Fable.Core.JsInterop
let viewC =
    React.functionComponent (fun (props: {| model: Model
                                            dispatch: Msg -> unit |}) ->
        let model = props.model
        let dispatch = props.dispatch

        let isCalculating =
            match model.Calculation with
            | Calculating -> true
            | _ -> false

        let (counterValue, setCounterValue) = React.useState (30)

        let scollDown() =
            match model.Calculation with
            | Calculated _ ->
                let element = document.querySelector("#myCanvas");
                element?scrollIntoView({| behavior= "smooth"; block= "end"|});
            | _ -> ()
            { new IDisposable with
                member this.Dispose() = ()
            }
        React.useEffect(scollDown, [|box isCalculating|])

        let subscribeToTimer () =
            // start the ticking
            setCounterValue 30

            let subscriptionId =
                JS.setTimeout (fun _ -> if isCalculating then setCounterValue (counterValue - 1)) 1000
            // return IDisposable with cleanup code
            { new IDisposable with
                member this.Dispose() = JS.clearTimeout (subscriptionId)
            }

        React.useEffect (subscribeToTimer, [| box isCalculating |])

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
                            Disabled = isCalculating
                        }
            ]

        let content =
            field.div [
                Html.b "How to use:"
                Html.ul
                    [
                        prop.style[style.listStyleType.circle]
                        let items =
                            [
                                "Enter container dimensions."
                                "Enter item dimensions."
                                "Add as many items as you want."
                                "All dimensions must be between 1 and 10000 and integer."
                                "Click calculate and wait up to 30 sec."
                                "Bin paker will try to fir the items and minimize the length."
                                "Review the result in 3D!"
                            ]
                        prop.children
                            [
                                for item in items do
                                    Html.li [
                                        prop.text item
                                    ]
                            ]
                    ]
                br []
                Bulma.label [
                    prop.text "Enter CONTAINER dimensions:"
                    control.isSmall
                ]
                Container.view
                    {
                        ContainerUpdated = fun r -> dispatch (ContainerUpdated(r))
                        Disabled = isCalculating
                    }

                Bulma.label [
                    prop.text "Enter ITEM dimensions:"
                    control.isSmall
                ]
                Html.div [
                    prop.className "table"
                    prop.disabled isCalculating
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

                let isinvalid =
                    (model.ContainerItem.IsNone
                     || model.TotalVolume.IsNone)

                Bulma.button.button [
                    prop.disabled (isinvalid || isCalculating)
                    color.isPrimary
                    prop.text
                        (if isCalculating
                         then sprintf "Calculating... (Max %i sec)" counterValue
                         else if isinvalid
                         then "First fill the form correctly"
                         else "Calculate")
                    prop.onClick (fun _ -> dispatch CalculateRequested)
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
                                                                                "Packs your bins for minimum length in the container"
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
            ])

let view model dispatch =
    viewC {| model = model; dispatch = dispatch |}
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
