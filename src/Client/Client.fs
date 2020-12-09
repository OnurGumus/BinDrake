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
open Feliz.Bulma.Operators
open Zanaptak.TypedCssClasses
open Fable.Core
open Shared.ClientModel
open Browser.Dom

type FA = CssClasses<"../../node_modules/@fortawesome/fontawesome-free/css/all.css", Naming.Underscores>
open Thoth.Json

[<Emit("btoa($0)")>]
let toBase64String (s: string): string = jsNative

[<Emit("atob($0)")>]
let fromBase64String (s: string): string = jsNative

let r = Random()

type Msg =
    | ResultLoaded of CalcResult list
    | ModelSaved of Guid
    | CalculateRequested
    | AddRow
    | RemoveItem of string
    | RowUpdated of string * RowItem option
    | ContainerUpdated of ContainerItem option
    | CalculationModeChanged of string
    | ContainerModeChanged of string
    | CurrentResultChanged of int
    | ShareRequested
    | ModelLoaded of Model

module Server =
    let logger: Shared.ILogger =
        { new Shared.ILogger with
            member this.LogError e = printf "%A" e
            member this.Log str arr = printf "%s" str
        }

    let rec sw () =
        { new IStopwatch with
            member this.ElapsedMilliseconds = 1L
            member this.StartNew() = sw ()
        }

    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api: ICalcApi =
        let calcApi =
            Remoting.createApi ()
            |> Remoting.withRouteBuilder Route.builder
            |> Remoting.buildProxy<ICalcApi>
        // { calcApi with
        //     run =
        //     fun calcs container items t alpha ->
        //         async {
        //             return
        //                 BinPacker.run
        //                     (sw ())
        //                     logger
        //                     container
        //                     calcs.ContainerMode
        //                     calcs.CalculationMode
        //                     items
        //                     t
        //                     alpha
        //     } }
        calcApi
let run = Server.api.run
let save = Server.api.saveModel
let load = Server.api.loadModel

let runCmd containerMode calcMode container items =
    Cmd.OfAsync.perform (fun _ ->
        run
            {
                ContainerMode = containerMode
                CalculationMode = (calcMode)
            }
            container
            items
            10000000000.
            0.99) () ResultLoaded

let saveCmd model =
    Cmd.OfAsync.perform (fun _ -> save model) () ModelSaved

let loadCmd guid =
    Cmd.OfAsync.perform (fun _ -> load guid) () ModelLoaded

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
            for i = 0L to 9L do
                {
                    Coord =
                        {
                            X = i * 10L
                            Y = Math.Abs(5L - i) * 10L
                            Z = Math.Abs(5L - i) * 10L
                        }
                    Item =
                        {
                            Dim = { Width = 10L; Height = 10L; Length = 10L }
                            Tag = colors.[int i]
                            Id = i.ToString()
                            NoTop = false
                            KeepTop = false
                            Weight = 0
                            KeepBottom = false
                        }
                }
            for i = 0L to 9L do
                {
                    Coord =
                        {
                            X = (i * 10L)
                            Y = Math.Abs(5L - i) * 10L
                            Z = 90L - Math.Abs(5L - i) * 10L
                        }
                    Item =
                        {
                            Dim = { Width = 10L; Height = 10L; Length = 10L }
                            Tag = colors.[int i]
                            Id = i.ToString()
                            NoTop = false
                            KeepTop = false
                            KeepBottom = false
                            Weight = 0
                        }
                }
        ]

    let container: Container =
        {
            Dim =
                {
                    Width = 100L
                    Height = 100L
                    Length = 100L
                }
            Coord = { X = 0L; Y = 0L; Z = 0L }
            Weight = 0
        }

    let cmd, loading =
        try
            let window = window.top

            match window.location.search with
            | null
            | "" -> Cmd.none, false
            | s when s.StartsWith("?g=") ->

                let guid =
                    Browser.Dom.window.location.search.Substring(3)
                    |> Guid.Parse

                (loadCmd guid), true
            | _ -> Cmd.none, false
        with e ->
            console.log e
            Cmd.none, false

    CanvasRenderer.renderResult container boxes true

    {
        Calculation = NotCalculated
        Container = None
        ContainerItem = None
        RowItems = [ newRowItem () ]
        TotalVolume = None
        CalculationMode = CalculationMode.MinimizeLength
        ContainerMode = ContainerMode.SingleContainer
        CurrentResultIndex = 0
        UrlShown = false
        Loading = loading
    },
    cmd

let cols =
    [
        "Length"
        "Width"
        "Height"
        "Weight"
        "Quant."
        "⬆⬆"
        "⬇⬇"
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
                yield
                    {
                        Tag = r.Color
                        Id = key + i.ToString()
                        NoTop = not (r.Stackable)
                        Dim =
                            {
                                Width = r.Width
                                Height = r.Height
                                Length = r.Length
                            }
                        Weight = r.Weight
                        KeepTop = r.KeepTop
                        KeepBottom = r.KeepBottom
                    }
    ]

let update (msg: Msg) model =
    let model, cmd =
        match msg with
        | ModelLoaded model ->
            match model.Calculation with
            | Calculated c -> { model with Loading = false }, Cmd.ofMsg (ResultLoaded c)
            | _ -> failwith "should not happen"

        | ModelSaved guid ->
            { model with UrlShown = true },
            Cmd.ofSub (fun _ -> Browser.Dom.window.history.replaceState (null, null, sprintf "?g=%s" (guid.ToString())))

        | ShareRequested -> model, saveCmd model
        | CurrentResultChanged i ->
            match model.Calculation with
            | Calculated c -> { model with CurrentResultIndex = i }, Cmd.ofMsg (ResultLoaded c)
            | _ -> failwith "should not happen"
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
            let model =
                { model with
                    Calculation = Calculated c
                }

            model,
            Cmd.ofSub (fun _ ->
                CanvasRenderer.renderResult
                    c.[model.CurrentResultIndex].Container
                    c.[model.CurrentResultIndex].ItemsPut
                    false)

        | ContainerUpdated c -> { model with ContainerItem = c }, Cmd.none
        | CalculationModeChanged "Minimize Length" ->
            { model with
                CalculationMode = MinimizeLength
            },
            Cmd.none
        | CalculationModeChanged "Minimize Height" ->
            { model with
                CalculationMode = MinimizeHeight
            },
            Cmd.none
        | CalculationModeChanged _ -> model, Cmd.none

        | ContainerModeChanged "Single Container" ->
            { model with
                ContainerMode = SingleContainer
            },
            Cmd.none

        | ContainerModeChanged "Multi Container" ->
            { model with
                ContainerMode = MultiContainer
            },
            Cmd.none

        | ContainerModeChanged _ -> model, Cmd.none
        | CalculateRequested _ ->
            let c = model.ContainerItem.Value

            let container: Container =
                {
                    Coord = { X = 0L; Y = 0L; Z = 0L }
                    Dim =
                        {
                            Width = c.Width
                            Height = c.Height
                            Length = c.Length
                        }
                    Weight = c.Weight
                }

            let items: list<Item> = convertToItems model

            { model with
                Calculation = Calculating
                CurrentResultIndex = 0
                UrlShown = false
            },
            runCmd model.ContainerMode model.CalculationMode container items

    let totalVolume =
        match model.RowItems with
        | rowItems when rowItems |> List.forall (fun x -> (fst x).IsSome) ->
            let rowItems =
                rowItems |> List.map (fun x -> (x |> fst).Value)

            let vol =
                rowItems
                |> List.sumBy (fun x -> x.Width * x.Height * x.Length * int64(x.Quantity))

            Some vol
        | _ -> None

    { model with TotalVolume = totalVolume }, cmd

type RowFormData =
    {
        Width: string
        Height: string
        Length: string
        Quantity: string
        Weight: string
        Color: string
        Stackable: bool
        KeepTop: bool
        KeepBottom: bool
    }


type RowProp =
    {
        RowUpdated: RowItem option -> unit
        AddRow: (unit -> unit) option
        Remove: (unit -> unit) option
        Key: string
        Disabled: bool
        FormData: RowFormData
    }

type ContainerFormData =
    {
        Width: string
        Height: string
        Length: string
        Weight: string
    }

type ContainerProp =
    {
        ContainerUpdated: ContainerItem option -> unit
        Disabled: bool
        ContainerFormData: ContainerFormData
    }

module Container =
    open Feliz.UseElmish

    type Model =
        {
            ContainerItem: Result<ContainerItem, Map<string, string list>> option
            FormData: ContainerFormData
        }

    type Msg =
        | WidthChanged of string
        | HeightChanged of string
        | LengthChanged of string
        | WeightChanged of string


    let init (formData: ContainerFormData) =
        {
            ContainerItem = None
            FormData = formData
        },
        Cmd.none

    let validate (formData: ContainerFormData) =
        all
        <| fun t ->
            let floatCheck = numericCheck t int64 0L 2000L
            let intCheck = numericCheck t int64 0L 2000L
            let weightCheck = numericCheck t int -1 100000

            {
                Width = floatCheck "width" formData.Width
                Height = floatCheck "height" formData.Height
                Length = floatCheck "length" formData.Length
                Weight = weightCheck "weight" formData.Weight
            }: ContainerItem

    let update containerUpdated msg (state: Model) =
        let formData = state.FormData

        let formData =
            match msg with
            | WidthChanged s -> { formData with Width = s }
            | HeightChanged s -> { formData with Height = s }
            | LengthChanged s -> { formData with Length = s }
            | WeightChanged s -> { formData with Weight = s }

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
                    React.useElmish (init props.ContainerFormData, update props.ContainerUpdated, [||])

                let dispatch' col v =
                    match col with
                    | "Height" -> HeightChanged v
                    | "Width" -> WidthChanged v
                    | "Length" -> LengthChanged v
                    | "Max Weight" -> WeightChanged v
                    | other -> failwith other
                    |> dispatch

                let defaultValue col =
                    match col with
                    | "Height" -> model.FormData.Height
                    | "Width" -> model.FormData.Width
                    | "Length" -> model.FormData.Length
                    | "Max Weight" ->
                        match model.FormData.Weight with
                        | null
                        | "" -> "0"
                        | e -> e
                    | other -> failwith other

                Html.div [
                    let cols =
                        [
                            "Length"
                            "Width"
                            "Height"
                            "Max Weight"
                        ]

                    prop.className "table"

                    prop.children [
                        Html.div [
                            prop.className "tr"
                            prop.children [
                                for col in cols do
                                    Html.div [
                                        prop.classes [ "td"; "th" ]
                                        prop.children [
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
                            prop.children [
                                for col in cols do
                                    control.div [
                                        prop.className "td"
                                        prop.children [
                                            input.number [
                                                prop.readOnly props.Disabled
                                                prop.maxLength 4
                                                prop.max 2000
                                                prop.defaultValue (defaultValue col)
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


    type Model =
        {
            RowItem: Result<RowItem, Map<string, string list>> option
            FormData: RowFormData
        }

    type Msg =
        | WidthChanged of string
        | HeightChanged of string
        | LengthChanged of string
        | WeightChanged of string
        | StackableChanged of bool
        | KeepBottomChanged of bool
        | TopChanged of bool
        | QuantityChanged of string



    let init (formData: RowFormData) =
        { RowItem = None; FormData = formData }, Cmd.none

    let validate (formData: RowFormData) =
        all
        <| fun t ->
            let floatCheck = numericCheck t int64 0L 2000L
            let intCheck = numericCheck t int 0 2000
            let weightCheck = numericCheck t int -1 100000

            {
                Width = floatCheck "width" formData.Width
                Height = floatCheck "height" formData.Height
                Length = floatCheck "length" formData.Length
                Quantity = intCheck "quantity" formData.Quantity
                Weight = weightCheck "weight" formData.Weight
                Stackable = formData.Stackable
                KeepTop = formData.KeepTop
                KeepBottom = formData.KeepBottom
                Color = formData.Color
            }: RowItem

    let update rowUpdated msg (state: Model) =
        let formData = state.FormData

        let formData =
            match msg with
            | WidthChanged s -> { formData with Width = s }
            | HeightChanged s -> { formData with Height = s }
            | LengthChanged s -> { formData with Length = s }
            | WeightChanged s -> { formData with Weight = s }
            | QuantityChanged s -> { formData with Quantity = s }
            | StackableChanged s -> { formData with Stackable = s }
            | KeepBottomChanged s -> { formData with KeepBottom = s }
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
                    React.useElmish (init props.FormData, update props.RowUpdated, [||])

                let removeButton =
                    Bulma.button.button [
                        button.isSmall ++ color.isDanger
                        prop.disabled props.Disabled
                        prop.classes [
                            FA.fa
                            FA.fa_times_circle
                        ]
                        match props.Remove with
                        | Some remove -> prop.onClick (fun _ -> remove ())
                        | _ -> prop.style [ style.visibility.hidden ]
                    ]

                let addButton =
                    Bulma.button.button [
                        button.isSmall ++ color.isPrimary
                        prop.disabled props.Disabled

                        prop.classes [
                            FA.fa
                            FA.fa_plus_circle
                        ]
                        match props.AddRow with
                        | Some addRow -> prop.onClick (fun _ -> addRow ())
                        | _ -> prop.style [ style.visibility.hidden ]
                    ]

                let dispatch' col v =
                    match col with
                    | "Height" -> HeightChanged v
                    | "Width" -> WidthChanged v
                    | "Weight" -> WeightChanged v
                    | "Quant." -> QuantityChanged v
                    | "⬆⬆" -> TopChanged(Boolean.Parse v)
                    | "⬇⬇" -> KeepBottomChanged(Boolean.Parse v)
                    | "Stack" -> StackableChanged(Boolean.Parse v)
                    | "Length" -> LengthChanged v
                    | other -> failwith other
                    |> dispatch

                let defaultt col =
                    match col with
                    | "Height" -> model.FormData.Height.ToString()
                    | "Width" -> model.FormData.Width.ToString()
                    | "Weight" -> model.FormData.Weight.ToString()
                    | "Quant." -> model.FormData.Quantity.ToString()
                    | "⬆⬆" -> model.FormData.KeepTop.ToString()
                    | "⬇⬇" -> model.FormData.KeepBottom.ToString()
                    | "Stack" -> model.FormData.Stackable.ToString()
                    | "Length" -> model.FormData.Length.ToString()
                    | other -> failwith other

                Html.div [
                    prop.className "tr"
                    prop.children [
                        for i, col in cols |> List.indexed do
                            control.div [
                                prop.className "td"
                                prop.children [
                                    if i < cols.Length - 2 then
                                        match col with
                                        | "⬆⬆" ->
                                            input.checkbox [
                                                input.isSmall
                                                prop.defaultChecked model.FormData.KeepTop
                                                prop.readOnly props.Disabled
                                                prop.onCheckedChange (fun e -> dispatch' "⬆⬆" (e.ToString()))
                                            ]
                                        | "⬇⬇" ->
                                            input.checkbox [
                                                input.isSmall
                                                prop.defaultChecked model.FormData.KeepBottom
                                                prop.readOnly props.Disabled
                                                prop.onCheckedChange (fun e -> dispatch' "⬇⬇" (e.ToString()))
                                            ]
                                        | "Stack" ->
                                            input.checkbox [
                                                input.isSmall
                                                prop.readOnly props.Disabled
                                                prop.defaultChecked model.FormData.Stackable
                                                prop.onCheckedChange (fun e -> dispatch' "Stack" (e.ToString()))
                                            ]
                                        | "Color" ->
                                            input.text [
                                                input.isSmall
                                                prop.readOnly true
                                                prop.style [
                                                    style.backgroundColor model.FormData.Color
                                                ]
                                            ]
                                        | _ ->
                                            input.number [
                                                prop.maxLength 5
                                                prop.readOnly props.Disabled
                                                prop.defaultValue (defaultt col)
                                                prop.max 2000
                                                input.isSmall
                                                prop.placeholder col
                                                prop.onChange (fun (e: Browser.Types.Event) -> dispatch' col e.Value)
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

let thousands (n:Int64) =
    let v = (if n < 0L then -n else n).ToString()
    let r = v.Length % 3
    let s = if r = 0 then 3 else r

    [
        yield v.[0..s - 1]
        for i in 0 .. (v.Length - s) / 3 - 1 do
            yield v.[i * 3 + s..i * 3 + s + 2]
    ]
    |> String.concat ","
    |> fun s -> if n < 0L then "-" + s else s

let viewC =
    React.functionComponent (fun (props: {| model: Model
                                            dispatch: Msg -> unit |}) ->
        let model = props.model
        let dispatch = props.dispatch

        let isCalculating =
            match model.Calculation with
            | Calculating -> true
            | _ -> false

        let (counterValue, setCounterValue) = React.useState (90)

        let scollDown () =
            match model.Calculation with
            | Calculated results when results
                                      |> List.exists (fun r -> r.ItemsPut.Length > 0) ->
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
            let subscriptionId =
                JS.setTimeout (fun _ -> if isCalculating then setCounterValue (counterValue - 1)) 1000
            { new IDisposable with
                member this.Dispose() = JS.clearTimeout (subscriptionId)
            }

        React.useEffect (subscribeToTimer)

        let rowItems =
            [
                for i, (row, key) in model.RowItems |> List.indexed do
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
                            FormData =
                                match row with
                                | None ->
                                    {
                                        Width = ""
                                        Height = ""
                                        Length = ""
                                        Weight = "0"
                                        Color =
                                            sprintf
                                                "rgb(%i,%i,%i)"
                                                (r.Next(40, 256))
                                                (r.Next(40, 256))
                                                (r.Next(40, 256))
                                        Stackable = true
                                        KeepTop = false
                                        KeepBottom = false
                                        Quantity = ""

                                    }
                                | Some r ->
                                    {
                                        Width = r.Width.ToString()
                                        Height = r.Height.ToString()
                                        Length = r.Length.ToString()
                                        Weight = r.Weight.ToString()
                                        Color = r.Color.ToString()
                                        Stackable = r.Stackable
                                        KeepTop = r.KeepTop
                                        KeepBottom = r.KeepBottom
                                        Quantity = r.Quantity.ToString()
                                    }

                        }
            ]

        let content =

            field.div [
                Html.b "How to use:"
                Html.ul [
                    spacing.ml1 ++ size.isSize7
                    prop.style [ style.listStyleType.disc ]

                    let items =
                        [
                            "Enter container and item dimensions between 1 and 2000, no decimals."
                            "Weight range is between 0 and 100,000."
                            "Add as many items as you want."
                            "If the item is not stackable (no other item is on top of this) uncheck \"Stack\" for that item."
                            "If the item must keep its upright then check \"⬆⬆\" for that item."
                            "If the item must be at the bottom (e.g, heavy items) then check \"⬇⬇\" for that item."
                            "All dimensions are unitless."
                            "Select the calculation mode depending on items to be at minimum height or pushed to the edge."
                            "Select container mode to multi container if you want to see how many container it takes to fit"
                            "Click calculate and wait up to 90 sec."
                            "Bin packer will try to fit the items and minimize the placement."
                            "Gravity is ignored."
                            "Review the result in 3D then you may share it via share the result button and copy the url."
                            "You may visually remove some boxes by using h-filter and v-filter controls on 3D."


                        ]

                    prop.children [
                        for item in items do
                            Html.li [ prop.text item ]
                    ]


                ]
                br []
                Bulma.label [
                    prop.text "Enter CONTAINER dimensions:"
                    control.isSmall
                ]
                if model.Loading then
                    Html.none
                else
                    match model.ContainerItem with
                    | None ->
                        Container.view
                            {
                                ContainerUpdated = fun r -> dispatch (ContainerUpdated(r))
                                Disabled = isCalculating
                                ContainerFormData =
                                    {
                                        Width = ""
                                        Height = ""
                                        Weight = "0"
                                        Length = ""
                                    }
                            }
                    | Some container ->
                        Container.view
                            {
                                ContainerUpdated = fun r -> dispatch (ContainerUpdated(r))
                                Disabled = isCalculating
                                ContainerFormData =
                                    {
                                        Width = container.Width.ToString()
                                        Height = container.Height.ToString()
                                        Weight = container.Weight.ToString()
                                        Length = container.Length.ToString()
                                    }
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
                            prop.children [
                                for col in cols do
                                    Html.div [
                                        prop.classes [ "td"; "th" ]
                                        prop.children [
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

                let line (title: string) (v: Int64 option) =
                    React.fragment [
                        Bulma.label title
                        control.div [
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
                | Calculated r -> line "Volume fit:" (Some(r |> List.sumBy (fun c -> c.PutVolume)))
                | _ -> Html.none

                let isMultiBin =
                    match model.ContainerMode with
                    | MultiContainer -> true
                    | _ -> false

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
                    |> (&&) (isMultiBin |> not)

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
                            | [] -> 0L
                            | other ->
                                (other |> List.maxBy (fun x -> x.Dim.Height))
                                    .Dim.Height

                        (areaItems > containerArea
                         || maxHeight > container.Height)
                        && (isMultiBin |> not)


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
                        || items
                           |> List.exists (fun i -> i.Weight > container.Weight)
                    | _ -> false

                Html.br []

                Html.div [
                    helpers.isInlineBlock
                    prop.children [
                        Bulma.label "Calculation mode:"
                        Html.select [
                            prop.value
                                (match model.CalculationMode with
                                 | MinimizeHeight -> "Minimize Height"
                                 | _ -> "Minimize Length")
                            prop.children [
                                Html.option "Minimize Height"
                                Html.option "Minimize Length"
                            ]
                            prop.onChange (fun (e: Event) ->
                                CalculationModeChanged(!!e.target?value)
                                |> dispatch)
                        ]
                    ]
                ]

                Html.div [
                    spacing.ml4 ++ helpers.isInlineBlock
                    prop.children [
                        Bulma.label "Container mode:"
                        Html.select [
                            prop.value
                                (match model.ContainerMode with
                                 | SingleContainer -> "Single Container"
                                 | _ -> "Multi Container")

                            prop.children [
                                Html.option "Single Container"
                                Html.option "Multi Container"
                            ]

                            prop.onChange (fun (e: Event) -> ContainerModeChanged(!!e.target?value) |> dispatch)
                        ]
                    ]
                ]

                Html.br []
                Html.br []

                Bulma.button.button [
                    prop.disabled
                        (isinvalid
                         || isCalculating
                        // || nostackExceeds
                         || volumeExceeds
                         || itemExceeds)
                    color.isPrimary
                    prop.id "calculate-button"
                    prop.text
                        (if isCalculating then
                            sprintf "Calculating... (Max %i sec)" counterValue

                         elif volumeExceeds then
                             "Items' volume exceeds single container volume."
                        //  else if nostackExceeds then
                        //      "No stack items won't fit to single container."
                         elif isinvalid then
                             "First fill the form correctly!"
                         else if itemExceeds then
                             "An item's parameters are larger than container's."

                         else
                             "Calculate")
                    prop.onClick (fun _ ->
                        setCounterValue 90
                        dispatch CalculateRequested)
                ]

                Html.span [
                    spacing.my1
                    prop.children [
                        match model.Calculation with
                        | Calculated c ->
                            let itemsPut = c |> List.collect (fun l -> l.ItemsPut)
                            let itemsUnput = (c |> List.last).ItemsUnput

                            let label =
                                match itemsUnput, itemsPut with
                                | [], _ ->
                                    Bulma.label [
                                        prop.style [ style.color.green ]
                                        prop.text "All items put successfully!"
                                    ]
                                | _, [] ->
                                    Bulma.label [
                                        color.isDanger
                                        prop.text "Unable to fit all items!"
                                    ]
                                | items, _ ->
                                    let g = items |> List.groupBy (fun x -> x.Tag)

                                    React.fragment [
                                        Bulma.label "Could not fit the following items:"
                                        Html.ul [
                                            for key, values in g do
                                                yield
                                                    Html.li [
                                                        Html.span [
                                                            helpers.isInlineBlock ++ color.hasTextWhite
                                                            prop.text " x "
                                                            prop.style [
                                                                style.backgroundColor key
                                                                style.width (length.ch 1)
                                                            ]
                                                        ]
                                                        Html.span [
                                                            prop.textf "%i items not fit with this color." values.Length
                                                        ]
                                                    ]
                                        ]

                                    ]

                            React.fragment [
                                label
                                Bulma.button.button [
                                    color.isInfo
                                    prop.text
                                        (if model.UrlShown then "Now copy the url and share it" else "Share the result")
                                    prop.disabled (isCalculating || model.UrlShown)
                                    prop.onClick (fun _ -> dispatch ShareRequested)
                                ]
                            ]
                        | _ -> Html.none

                    ]

                ]

                match model.Calculation with
                | Calculated c ->
                    Html.div [
                        Html.br []
                        Bulma.button.button [
                            color.isDanger
                            prop.text " << "
                            prop.disabled ((model.CurrentResultIndex = 0) || isCalculating)
                            prop.onClick (fun _ -> dispatch (CurrentResultChanged(model.CurrentResultIndex - 1)))
                        ]
                        let containers =
                            Html.span [
                                text.hasTextWeightSemibold
                                prop.textf "Showing container: %i/%i" (model.CurrentResultIndex + 1) (c.Length)
                            ]

                        match model.Calculation with
                        | Calculated c ->
                            Html.span [
                                spacing.mx2
                                prop.style [
                                    style.display.inlineFlex
                                    style.flexDirection.column
                                ]
                                prop.children [

                                    containers
                                    Html.span [
                                        text.hasTextWeightSemibold
                                        if (c.[model.CurrentResultIndex].ItemsPut).Length > 0 then
                                            prop.textf
                                                "Max item L:%i, H:%i"
                                                ((c.[model.CurrentResultIndex].ItemsPut
                                                  |> List.map (fun i -> i.Coord.Z + i.Item.Dim.Length))
                                                 |> List.max)
                                                ((c.[model.CurrentResultIndex].ItemsPut
                                                  |> List.map (fun i -> i.Coord.Y + i.Item.Dim.Height))
                                                 |> List.max)
                                    ]
                                ]
                            ]
                        | _ -> containers

                        Bulma.button.button [
                            color.isDanger
                            prop.text " >> "
                            prop.disabled
                                ((model.CurrentResultIndex = c.Length - 1)
                                 || isCalculating)
                            prop.onClick (fun _ -> dispatch (CurrentResultChanged(model.CurrentResultIndex + 1)))

                        ]

                    ]
                | _ -> Html.none


            ]

        Bulma.container [
            Bulma.columns [
                Bulma.column [
                    Bulma.panel [
                        spacing.mt1
                        prop.children [
                            Bulma.panelHeading [
                                Html.h1 [
                                    prop.style [ style.color.white ]
                                    prop.text "3D Bin Packer"
                                ]
                            ]
                            Bulma.panelBlock.div [ content ]
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
