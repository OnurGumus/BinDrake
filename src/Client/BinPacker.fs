module BinPacker

[<Struct>]
type Coordinates = { X: int; Y: int; Z: int }

[<Struct>]
type Dim =
    {
        Width: int
        Height: int
        Length: int
    }

type Container = { Dim: Dim; Coord: Coordinates }
type Item = { Dim: Dim; Id: string; Tag: string }
type ContainerTriplet = Container list
type ItemPut = { Item: Item; Coord: Coordinates }
type PutResult = (ContainerTriplet list * ItemPut) option
type ItemsWithCost = { Items: Item list; Cost: float }
type PutItem = Container -> Item -> PutResult
type StackItem = (Container list * Item list * ItemPut list)
let random = System.Random()

let inline rotateZ (item: Item) =
    { item with
        Dim =
            {
                Height = item.Dim.Width
                Width = item.Dim.Height
                Length = item.Dim.Length
            }
    }

let inline rotateY (item: Item) =
    { item with
        Dim =
            {
                Height = item.Dim.Height
                Width = item.Dim.Length
                Length = item.Dim.Width
            }
    }

let inline rotateX (item: Item) =
    { item with
        Dim =
            {
                Height = item.Dim.Length
                Width = item.Dim.Width
                Length = item.Dim.Height
            }
    }
let checkConflictC (A:Container) (B:Container) =
    not (B.Coord.X >= A.Coord.X+A.Dim.Width
        || B.Coord.X+B.Dim.Width <= A.Coord.X
        || B.Coord.Y >= A.Coord.Y+A.Dim.Height
        || B.Coord.Y+B.Dim.Height <= A.Coord.Y
        || B.Coord.Z >= A.Coord.Z+A.Dim.Length
        || B.Coord.Z+B.Dim.Length <= A.Coord.Z)
let containerssCheck (containers : Container list) =
    for item1 in containers do
       for item2 in containers do
           if item1 <> item2 && checkConflictC item1 item2 then
                printfn "contianer conflc %A %A" item1 item2
                failwith "conf"

let mergeContainers  (containers : Container list) =
    let containers = containers |> List.indexed
    let mergersZ containers =
        seq {for (i1,c1:Container) in containers do
                for (i2, c2) in containers do
                    if (i1 <> i2) then
                        if c1.Coord.X = c2.Coord.X
                            && c1.Coord.Y = c2.Coord.Y
                            && c1.Dim.Width = c2.Dim.Width
                            && c2.Dim.Height = c1.Dim.Height
                            && c2.Coord.Z = c1.Coord.Z + c1.Dim.Length then
                            ((i1,c1),(i2,c2))}

    let mergeContainersZ (c1: Container)  (c2:Container) =
        {   Dim = { Width = c1.Dim.Width; Height = c1.Dim.Height ; Length = c1.Dim.Length + c2.Dim.Length }
            Coord = c1.Coord }

    let mergersY containers =
        seq {for (i1,c1:Container) in containers do
                for (i2, c2) in containers do
                    if (i1 <> i2) then
                        if c1.Coord.X = c2.Coord.X
                            && c1.Coord.Z = c2.Coord.Z
                            && c1.Dim.Length = c2.Dim.Length
                            && c2.Dim.Width = c1.Dim.Width
                            && c2.Coord.Y = c1.Coord.Y + c1.Dim.Height then
                            ((i1,c1),(i2,c2))}



    let mergeContainersY (c1: Container)  (c2:Container) =
        {   Dim = { Length = c1.Dim.Length; Width = c1.Dim.Width ; Height = c1.Dim.Height + c2.Dim.Height }
            Coord = c1.Coord }

    let mergersX containers =
        seq {for (i1,c1:Container) in containers do
                for (i2, c2) in containers do
                    if (i1 <> i2) then
                        if c1.Coord.Y = c2.Coord.Y
                            && c1.Coord.Z = c2.Coord.Z
                            && c1.Dim.Length = c2.Dim.Length
                            && c2.Dim.Height = c1.Dim.Height
                            && c2.Coord.X = c1.Coord.X + c1.Dim.Width then
                            ((i1,c1),(i2,c2))}



    let mergeContainersX (c1: Container)  (c2:Container) =
        {   Dim = { Length = c1.Dim.Length; Height = c1.Dim.Height ; Width = c1.Dim.Width + c2.Dim.Width }
            Coord = c1.Coord }
    let removeAt index1 index2 input =
        input
        |> List.mapi (fun i el -> (i <> index1 && i <> index2, el))
        |> List.filter fst |> List.map snd

    let rec containerLoop merger containers mergeContainersf mergersf =
        if merger |> Seq.isEmpty then containers
        else
            let ((i1,c1),(i2,c2)) = merger |> Seq.head
            let newC = mergeContainersf c1 c2
            printf "%A" containers
            let containers = containers |> removeAt i1 i2
            let mergers = mergersf containers
            let naked = containers |> List.map snd
            containerssCheck naked
            containerLoop mergers ((newC::naked) |>List.indexed) mergeContainersf mergersf

    let init = mergersZ containers
    let containers =
        containerLoop init containers mergeContainersZ mergersZ
        |> List.map snd
        |> List.indexed
   // containers |> List.map snd |> containerssCheck
    // let init = mergersY containers

    // let containers =
    //     containerLoop init containers mergeContainersY mergersY
    //     |> List.map snd
    //     |> List.indexed

    // let init = mergersX containers

    // let containers =
    //     containerLoop init containers mergeContainersX mergersX
    //     |> List.map snd

  //  containers |> containerssCheck
    containers |> List.map snd



let rec putItem tryCount: PutItem =
    fun container item ->
        let remainingWidth = container.Dim.Width - item.Dim.Width
        let remainingHeight = container.Dim.Height - item.Dim.Height
        let remainingLength = container.Dim.Length - item.Dim.Length

        if (remainingHeight < 0)
           || remainingLength < 0
           || remainingWidth < 0 then
            if tryCount > 0 then
                if tryCount = 3 then
                    let item = item |> rotateZ

                    putItem (tryCount - 1) container item
                else if tryCount = 2 then
                    let item = item |> rotateY

                    putItem (tryCount - 1) container item
                else
                    let item = item |> rotateX

                    putItem (tryCount - 1) container item

            else
                None
        else
            let config1 =
                let topBlock =
                    {
                        Dim =
                            {
                                Width = container.Dim.Width
                                Height = remainingHeight
                                Length = item.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y + item.Dim.Height
                                Z = container.Coord.Z
                            }
                    }

                let sideBlock =
                    {
                        Dim =
                            {
                                Width = remainingWidth
                                Height = item.Dim.Height
                                Length = item.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X + item.Dim.Width
                                Y = container.Coord.Y
                                Z = container.Coord.Z
                            }
                    }

                let remainingBlock =
                    {
                        Dim =
                            {
                                Width = container.Dim.Width
                                Height = container.Dim.Height
                                Length = remainingLength
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y
                                Z = container.Coord.Z + item.Dim.Length
                            }
                    }

                [ topBlock; sideBlock; remainingBlock ]
                |> List.filter (fun s ->
                    s.Dim.Width > 0
                    && s.Dim.Height > 0
                    && s.Dim.Length > 0)
                |> List.sortBy (fun s -> s.Coord.Z)

            let config2 =
                let topBlock =
                    {
                        Dim =
                            {
                                Width = item.Dim.Width
                                Height = remainingHeight
                                Length = item.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y + item.Dim.Height
                                Z = container.Coord.Z
                            }
                    }

                let sideBlock =
                    {
                        Dim =
                            {
                                Width = remainingWidth
                                Height = container.Dim.Height
                                Length = item.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X + item.Dim.Width
                                Y = container.Coord.Y
                                Z = container.Coord.Z
                            }
                    }

                let remainingBlock =
                    {
                        Dim =
                            {
                                Width = container.Dim.Width
                                Height = container.Dim.Height
                                Length = remainingLength
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y
                                Z = container.Coord.Z + item.Dim.Length
                            }
                    }

                [ topBlock; sideBlock;  remainingBlock ]
                |> List.filter (fun s ->
                    s.Dim.Width > 0
                    && s.Dim.Height > 0
                    && s.Dim.Length > 0)
                |> List.sortBy (fun s -> s.Coord.Z)

            let config3 =
                let topBlock =
                    {
                        Dim =
                            {
                                Width = container.Dim.Width
                                Height = remainingHeight
                                Length = container.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y + item.Dim.Height
                                Z = container.Coord.Z
                            }
                    }

                let sideBlock =
                    {
                        Dim =
                            {
                                Width = remainingWidth
                                Height = item.Dim.Height
                                Length = container.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X + item.Dim.Width
                                Y = container.Coord.Y
                                Z = container.Coord.Z
                            }
                    }

                let remainingBlock =
                    {
                        Dim =
                            {
                                Width = item.Dim.Width
                                Height = item.Dim.Height
                                Length = remainingLength
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y
                                Z = container.Coord.Z + item.Dim.Length
                            }
                    }

                [  topBlock; sideBlock; remainingBlock ]
                |> List.filter (fun s ->
                    s.Dim.Width > 0
                    && s.Dim.Height > 0
                    && s.Dim.Length > 0)
                |> List.sortBy (fun s -> s.Coord.Z)

            let config4 =
                let topBlock =
                    {
                        Dim =
                            {
                                Width = item.Dim.Width
                                Height = remainingHeight
                                Length = container.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y + item.Dim.Height
                                Z = container.Coord.Z
                            }
                    }

                let sideBlock =
                    {
                        Dim =
                            {
                                Width = remainingWidth
                                Height = container.Dim.Height
                                Length = container.Dim.Length
                            }
                        Coord =
                            {
                                X = container.Coord.X + item.Dim.Width
                                Y = container.Coord.Y
                                Z = container.Coord.Z
                            }
                    }

                let remainingBlock =
                    {
                        Dim =
                            {
                                Width = item.Dim.Width
                                Height = item.Dim.Height
                                Length = remainingLength
                            }
                        Coord =
                            {
                                X = container.Coord.X
                                Y = container.Coord.Y
                                Z = container.Coord.Z + item.Dim.Length
                            }
                    }

                [ topBlock; sideBlock; remainingBlock ]
                |> List.filter (fun s ->
                    s.Dim.Width > 0
                    && s.Dim.Height > 0
                    && s.Dim.Length > 0)
                |> List.sortBy (fun s -> s.Coord.Z)

            let itemPut =
                {
                    Item = item
                    Coord =
                        {
                            X = container.Coord.X
                            Y = container.Coord.Y
                            Z = container.Coord.Z
                        }
                }
            let t = [config1;config2;config3; config4] |> List.item (random.Next(0,4))
            Some
                (  [t],
                 //|> List.filter (fun f -> f |> List.isEmpty |> not)

                // |> List.sortBy (fun s -> s |> List.minBy (fun x -> float x.Coord.Z)),
                 itemPut)

let checkConflictI itemsPut =
    let checkConflict (A:ItemPut) (B:ItemPut) =
        not (B.Coord.X >= A.Coord.X+A.Item.Dim.Width
            || B.Coord.X+B.Item.Dim.Width <= A.Coord.X
            || B.Coord.Y >= A.Coord.Y+A.Item.Dim.Height
            || B.Coord.Y+B.Item.Dim.Height <= A.Coord.Y
            || B.Coord.Z >= A.Coord.Z+A.Item.Dim.Length
            || B.Coord.Z+B.Item.Dim.Length <= A.Coord.Z)

    for item1 in itemsPut do
       for item2 in itemsPut do
           if item1.Item.Id <> item2.Item.Id && checkConflict item1 item2 then
                printfn "Items conflc %A %A" item1 item2
                failwith "wr"

let calculateCost =
    fun putItem containers items ->
        let rec loop =
            function
            | ( _ ,[], itemPuts)::_, _ -> Some itemPuts
            | (containerSet, item :: remainingItems, itemPuts) :: remainingStack, counter when counter > 0 ->
                let containerSet = containerSet //|> mergeContainers

                let rec loopContainers: (Container list * Container list) -> StackItem list  =
                    function
                    | (container :: remainingContainers), triedButNotFit ->
                        match putItem container item with
                        | Some (containerTriplets, (itemPut: ItemPut)) ->
                            let firstRes: StackItem list  =
                                [
                                    for triplet in containerTriplets do
                                        let rema = (remainingContainers @ triplet @ triedButNotFit)//|> List.tail //|> mergeContainers
                                        let newItems  = itemPut::itemPuts
                                        checkConflictI newItems
                                        yield (rema
                                               |> List.sortBy (fun s -> s.Coord.Z),
                                               remainingItems,
                                               newItems
                                               )
                                ]

                            firstRes
                            // let otherRes =
                            //     loopContainers
                            //         ((remainingContainers
                            //           |> List.sortBy (fun s -> s.Coord.Z)),
                            //          triedButNotFit)

                            // if (snd otherRes).IsNone
                            //    || (snd firstRes).Value.Coord.Z
                            //    <= (snd otherRes).Value.Coord.Z then
                            //     firstRes
                            // else
                            //     otherRes
                        | _ -> loopContainers (remainingContainers, container :: triedButNotFit)
                    | _ -> []

                let stackItems =
                    loopContainers ((containerSet |> List.sortBy (fun s -> s.Coord.Z)) , [])
                printf "loop"
                loop
                    ((stackItems@remainingStack)
                     |> List.sortBy (fun (x,y,z) ->  x|> List.minBy (fun l -> l.Coord.Z)),

                     counter - 1)

            | _, _ -> None

        loop ([ containers, items,[] ], 80)

let calcVolume (item: Item) =
    item.Dim.Width * item.Dim.Height * item.Dim.Length


let rotateToMinZ item =
    let x = item |> rotateX
    let y = item |> rotateY
    [item;x;y ] |> List.minBy(fun d -> d.Dim.Length)

let inline randomRotate item: Item =
    let r = random.NextDouble()
    if r < 0.333 then rotateZ item
    elif r < 0.666 then rotateY item
    else rotateX item

let inline mutate (itemsPut:ItemPut list) (items: Item list) =
    let firstSwap =
        if random.NextDouble() > 0.6 || itemsPut |> List.isEmpty
            then random.Next(0, items.Length)
        else
           let maxId = itemsPut |> List.maxBy(fun x -> x.Coord.Z)
           items |> List.findIndex (fun x-> x.Id = maxId.Item.Id)

    let secondSwap = random.Next(0, items.Length)
    let arr = items |> Array.ofList
    let tmp = arr.[firstSwap] |> randomRotate
    arr.[firstSwap] <- arr.[secondSwap] //|> randomRotate
    arr.[secondSwap] <- tmp
    arr |> List.ofArray

let TMin = 0.01
open System

let inline findUnfitItems itemsPut (items:Item list) =
    let itemsPutIds = itemsPut |> List.map(fun d -> d.Item.Id)
    items |> List.filter(fun i ->  itemsPutIds |> List.contains i.Id |> not)

let calcCost container items =
    match calculateCost (putItem 3) [ container ] (items |> List.sortByDescending calcVolume) with
    | Some res ->

        let unfitItems = findUnfitItems res items// printf "%A" unfitItems
        let maxZCoord =
            let max = (res |> List.maxBy (fun x -> x.Coord.Z + x.Item.Dim.Length))
            max.Item.Dim.Length + max.Coord.Z
        let sumZCoord =
            (res |> List.sumBy (fun x -> x.Coord.Z + x.Item.Dim.Length))
        float ((unfitItems |> List.sumBy calcVolume)*1000 + maxZCoord +  100  * sumZCoord) , res
    | None _ -> Double.MaxValue, []

type GlobalBest = { ItemsPut : ItemPut list; Cost : float}

let rec calc (container: Container) (itemsWithCost: ItemsWithCost) (globalBest:GlobalBest) (T: float) (alpha:float) result =
    if TMin >= T then
        globalBest
    else
        let items = itemsWithCost.Items

        let calculated, res , globalBest=
            if itemsWithCost.Cost = 0. then
                let cost, res = calcCost container items
                { itemsWithCost with Cost = cost }, res, { ItemsPut = res ; Cost = cost }
            else
                itemsWithCost, result, globalBest

        let rec loop (itemsWC: ItemsWithCost, res) (globalBest:GlobalBest)  count =
            if count = 0 then
                itemsWC, res, globalBest
            else
                let items = itemsWC.Items
                let nbr = items |> mutate res |> mutate res |> mutate res |> mutate res |> mutate  res
                let nbrCost, nbrRes = calcCost container nbr

                let nextItem, res , globalBest2=
                    printfn "costs: %f-%f" calculated.Cost nbrCost
                    if nbrCost < calculated.Cost then
                        { Items = nbr; Cost = nbrCost }, nbrRes,
                            if nbrCost < globalBest.Cost then
                                { ItemsPut = nbrRes ; Cost = nbrCost }
                            else globalBest
                    else
                        let diff = (calculated.Cost - nbrCost)
                        let exp = Math.Exp(diff / T)
                        if exp < 1.0 && exp > random.NextDouble() then
                            { Items = nbr; Cost = nbrCost }, nbrRes, globalBest
                        else
                            {
                                Items = items
                                Cost = calculated.Cost
                            },
                            res, globalBest

                loop (nextItem, res) globalBest2 (count - 1)

        let c, r, globalBest = loop (calculated, res) globalBest 1
        calc container c globalBest (T * alpha) alpha r

type CalcResult = {
    ItemsPut : ItemPut list
    ContainerVol : int
    ItemsUnput : Item list
    PutVolume : int
}

let checkConflict (A:ItemPut) (B:ItemPut) =
    not (B.Coord.X >= A.Coord.X+A.Item.Dim.Width
        || B.Coord.X+B.Item.Dim.Width <= A.Coord.X
        || B.Coord.Y >= A.Coord.Y+A.Item.Dim.Height
        || B.Coord.Y+B.Item.Dim.Height <= A.Coord.Y
        || B.Coord.Z >= A.Coord.Z+A.Item.Dim.Length
        || B.Coord.Z+B.Item.Dim.Length <= A.Coord.Z)

let run  (container: Container) (items: Item list) (T: float) (alpha:float) =
    let itemsWithCost = { Items = items |> List.map rotateToMinZ ; Cost = 0.}
    let itemsPut = (calc container itemsWithCost { ItemsPut = []; Cost = Double.MaxValue } T alpha []).ItemsPut
    let volumeContainer = container.Dim.Height * container.Dim.Width * container.Dim.Length
    let itemsUnput = findUnfitItems itemsPut items
    let putVolume = itemsUnput |> List.sumBy calcVolume
    for item1 in itemsPut do
       for item2 in itemsPut do
           if item1.Item.Id <> item2.Item.Id && checkConflict item1 item2 then
                printfn "Items conflc %A %A" item1 item2
    { ItemsPut = itemsPut; ContainerVol = volumeContainer; ItemsUnput = itemsUnput; PutVolume = putVolume }

