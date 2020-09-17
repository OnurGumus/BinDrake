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
        KeepTop : bool
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
    Cmd.OfAsync.perform (fun _ -> run container items 10000000000. 0.99) () ResultLoaded

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
                            KeepTop = false
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
                            KeepTop = false
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
        "Upright"
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

let convertToItems (model: Model) =
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
                          KeepTop = r.KeepTop
                      }
    ]

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

            let items: list<Item> = convertToItems model

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
            let floatCheck = numericCheck t int 0 2000
            let intCheck = numericCheck t int 0 2000
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
                                                            prop.maxLength 4
                                                            prop.max 2000
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
            KeepTop : bool
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
        | TopChanged of bool
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
                    Color = sprintf "rgb(%i,%i,%i)" (r.Next(40,256)) (r.Next(40,256)) (r.Next(40,256))
                    Stackable = true
                    KeepTop = false
                    Quantity = ""
                }
        },
        Cmd.none

    let validate (formData: FormData) =
        all
        <| fun t ->
            let floatCheck = numericCheck t int 0 2000
            let intCheck = numericCheck t int 0 2000
            {
                Width = floatCheck "width" formData.Width
                Height = floatCheck "height" formData.Height
                Length = floatCheck "length" formData.Length
                Quantity = intCheck "quantity" formData.Quantity
                Stackable = formData.Stackable
                KeepTop = formData.KeepTop
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
            | TopChanged s -> { formData with KeepTop = s }

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
                    | "Upright" -> TopChanged(Boolean.Parse v)
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
                                                | "Upright" ->
                                                input.checkbox [
                                                    input.isSmall
                                                    prop.readOnly props.Disabled
                                                    prop.onCheckedChange (fun e -> dispatch' "Upright" (e.ToString()))
                                                ]
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
                                                        prop.max 2000
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
let thousands n =
    let v = (if n < 0 then -n else n).ToString()
    let r = v.Length % 3
    let s = if r = 0 then 3 else r
    [   yield v.[0.. s - 1]
        for i in 0..(v.Length - s)/ 3 - 1 do
            yield v.[i * 3 + s .. i * 3 + s + 2]
    ]
    |> String.concat ","
    |> fun s -> if n < 0 then "-" + s else s
let viewC =
    React.functionComponent (fun (props: {| model: Model
                                            dispatch: Msg -> unit |}) ->
        let model = props.model
        let dispatch = props.dispatch

        let isCalculating =
            match model.Calculation with
            | Calculating -> true
            | _ -> false

        let (counterValue, setCounterValue) = React.useState (35)

        let scollDown () =
            match model.Calculation with
            | Calculated { ItemsPut = itemsPut } when itemsPut.Length > 0 ->
                let element =
                    document.querySelector ("#calculate-button")

                element?scrollIntoView ({|
                                            behavior = "smooth"
                                            block = "start"
                                        |})
            | _ -> ()
            { new IDisposable with
                member this.Dispose() = ()
            }

        React.useEffect (scollDown, [| box isCalculating |])

        let subscribeToTimer () =
            // start the ticking

            let subscriptionId =
                JS.setTimeout (fun _ ->
                    printf "%A" counterValue
                    if isCalculating then setCounterValue (counterValue - 1)) 1000
            // return IDisposable with cleanup code
            { new IDisposable with
                member this.Dispose() = JS.clearTimeout (subscriptionId)
            }

        React.useEffect (subscribeToTimer)

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
                Html.ul [
                    prop.style [
                        style.listStyleType.disc
                        style.marginLeft (length.em 1)
                        style.custom ("fontSize", "smaller")
                    ]

                    let items =
                        [
                            "Enter container and item dimensions between 1 and 2000, no decimals."
                            "Add as many items as you want."
                            "If the item is not stackable uncheck stack for that item."
                            "If the item must keep its upright then check upside for that item."
                            "All dimensions are unitless."
                            "Click calculate and wait up to 90 sec."
                            "Bin packer will try to fit the items and minimize the length."
                            "Review the result in 3D!"
                        ]

                    prop.children
                        [
                            for item in items do
                                Html.li [ prop.text item ]
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
                        Bulma.label title
                        control.div
                            [
                                Html.output [
                                    if title.StartsWith "Chargable" && v.IsSome
                                    then prop.className "output"
                                    prop.text
                                        (v
                                         |> Option.map (thousands)
                                         |> Option.defaultValue "Please complete the form.")
                                ]
                            ]
                    ]

                [
                    let items =
                        [
                            ("Total Item Volume:", model.TotalVolume)
                        ]

                    for t, v in items do
                        line t v
                ]
                |> ofList

                match model.ContainerItem with
                | Some container ->
                    line
                        "Container volume:"
                        (Some
                            (container.Height
                             * container.Width
                             * container.Length))
                | _ -> Html.none

                match model.Calculation with
                | Calculated r ->
                    line "Volume fit:"  (Some r.PutVolume)
                | _ -> Html.none

                let isinvalid =
                    (model.ContainerItem.IsNone
                     || model.TotalVolume.IsNone)

                let volumeExceeds =
                    match model.ContainerItem, model.TotalVolume with
                    | Some container, Some volume ->
                        container.Height
                        * container.Width
                        * container.Length < volume
                    | _ -> false

                let nostackExceeds =
                    match model.ContainerItem, model.TotalVolume with
                    | Some container, Some volume ->
                        let containerArea = container.Length * container.Width

                        let areaItems =
                            model
                            |> convertToItems
                            |> List.filter (fun x -> x.NoTop)
                            |> List.sumBy (fun x -> x.Dim.Width * x.Dim.Length)
                        let maxHeight =
                            model
                            |> convertToItems
                            |> List.filter (fun x -> x.NoTop)
                            |> function
                               | [] -> 0
                               | other -> (other |> List.maxBy (fun x -> x.Dim.Height)).Dim.Height

                        areaItems > containerArea || maxHeight > container.Height


                    | _ -> false

                let itemExceeds =
                    match model.ContainerItem, model.TotalVolume with
                    | Some container, Some volume ->
                        let checkDim (item: Item) =
                            let itemDim =
                                Math.Max(Math.Max(item.Dim.Length, item.Dim.Width), item.Dim.Height)

                            let cDim =
                                Math.Max(Math.Max(container.Length, container.Width), container.Height)

                            itemDim > cDim

                        let items = convertToItems model
                        List.exists (checkDim) items
                    | _ -> false

                Bulma.button.button [
                    prop.disabled
                        (isinvalid
                         || isCalculating
                         || nostackExceeds
                         || volumeExceeds
                         || itemExceeds)
                    color.isPrimary
                    prop.id "calculate-button"
                    prop.text
                        (if isCalculating
                         then sprintf "Calculating... (Max %i sec)" counterValue
                         else if isinvalid
                         then "First fill the form correctly!"
                         else if volumeExceeds
                         then "Items' volume exceeds container volume."
                         else if nostackExceeds
                         then "No stack items won't fit to container."
                         else if itemExceeds
                         then "An item's dim is larger than container."

                         else "Calculate")
                    prop.onClick (fun _ ->
                        setCounterValue 90
                        dispatch CalculateRequested)
                ]
                Html.span [
                    spacing.my1
                    prop.children
                        [
                            match model.Calculation with
                            | Calculated c ->

                                match c.ItemsUnput, c.ItemsPut with
                                | [], _ ->
                                    Bulma.label [
                                        prop.style [ style.color "green" ]
                                        prop.text "All items put successfully!"
                                    ]
                                | _, [] ->
                                    Bulma.label [
                                        prop.style [ style.color "red" ]
                                        prop.text "Unable to fit all items!"
                                    ]
                                | items, _ ->
                                    let g = items |> List.groupBy (fun x -> x.Tag)
                                    React.fragment [
                                        Bulma.label "Could not fit the following items:"
                                        Html.ul
                                            [
                                                prop.children
                                                    [
                                                        for key, values in g do
                                                            yield Html.li
                                                                      [
                                                                          prop.children [
                                                                              Html.span [
                                                                                  prop.text " x "
                                                                                  prop.style [
                                                                                      style.backgroundColor key
                                                                                      style.color.white
                                                                                      style.width (length.ch 1)
                                                                                      style.display.inlineBlock
                                                                                  ]
                                                                              ]
                                                                              Html.span
                                                                                  [
                                                                                      prop.text
                                                                                          (sprintf
                                                                                              "%i items not fit with this color."
                                                                                               values.Length)
                                                                                  ]
                                                                          ]
                                                                      ]
                                                    ]
                                            ]
                                    ]
                            | _ -> Html.none
                        ]
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
                                                            spacing.mt1
                                                            prop.children [
                                                                Bulma.panelHeading
                                                                    [
                                                                        Html.span [
                                                                            prop.style [ style.color.white ]
                                                                            prop.text "3D Bin Packer"
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
