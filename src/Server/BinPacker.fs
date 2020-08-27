module BinPacker
open Shared
open System

let random = System.Random()
let dimToVolume (d:Dim)=
    d.Width * d.Height * d.Length

let inline rotateZ (item: Item) =
    if item.NoTop then item else

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
    if item.NoTop then item else
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

    let rec containerLoop merger containers mergeContainersf mergersf added =
        if merger |> Seq.isEmpty then containers@(added |> List.indexed)
        else
            let ((i1,c1),(i2,c2)) = merger |> Seq.head
            let newC = mergeContainersf c1 c2
            let prev = containers.Length
            let containers = containers |> removeAt i1 i2
            let containers = containers |> List.map snd |> List.indexed
            let mergers = mergersf containers
            containerLoop mergers ((containers)) mergeContainersf mergersf (newC::added)

    let init = mergersZ containers
    let containers =
        containerLoop init containers mergeContainersZ mergersZ []
        |> List.map snd
        |> List.indexed

    let init = mergersY containers

    let containers =
         containerLoop init containers mergeContainersY mergersY []
         |> List.map snd
         |> List.indexed

    let init = mergersX containers
    let containers =
         containerLoop init containers mergeContainersX mergersX []
        |> List.map snd

    containers |> containerssCheck

    containers

let rec putItem (rootContainer:Container)  tryCount :PutItem =
    fun container item  ->
        let remainingWidth = container.Dim.Width - item.Dim.Width
        let remainingHeight = container.Dim.Height - item.Dim.Height
        let remainingLength = container.Dim.Length - item.Dim.Length
        if item.NoTop && container.Coord.Y + container.Dim.Height < rootContainer.Dim.Height then
            None
        elif (remainingHeight < 0)
           || remainingLength < 0
           || remainingWidth < 0 then
            if tryCount > 0 then
                if tryCount = 3 then
                    let item = item |> rotateZ

                    putItem rootContainer (tryCount - 1) container item
                else if tryCount = 2 then
                    let item = item |> rotateY

                    putItem rootContainer (tryCount - 1) container item
                else
                    let item = item |> rotateX

                    putItem rootContainer (tryCount - 1) container item

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
                |> List.filter(fun s -> (item.NoTop && s.Coord.Y > 0)|>not)
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
                |> List.filter(fun s -> (item.NoTop && s.Coord.Y > 0)|>not)
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
                |> List.filter(fun s -> (item.NoTop && s.Coord.Y > 0)|>not)
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
                |> List.filter(fun s -> (item.NoTop && s.Coord.Y > 0)|>not)
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
            let t1 = [config1;config2;config3;config4] |> List.item (random.Next(0,4))
            let t2 = [config1;config2;config3; config4] |> List.item (random.Next(0,4))
            let res =
                Some
                    (  [t1;t2] |> List.distinct
                       |> List.filter (fun f -> f |> List.isEmpty |> not)

                        |> List.sortBy (fun s -> (s |> List.minBy (fun x -> float x.Coord.Z)).Coord.Z ),
                     itemPut)
            res

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
            | ( containerSet ,[], itemPuts)::_, _ -> Some (containerSet,itemPuts)
            | (containerSet, item :: remainingItems, itemPuts) :: remainingStack, counter when counter > 0 ->
                let containerSet = containerSet |> mergeContainers
                let rec loopContainers: (Container list * Container list) -> StackItem list  =
                    function
                    | (container :: remainingContainers) as cs, triedButNotFit ->
                        //printfn "cs:%A, item: %A" cs item
                        //printfn "!!!"

                        match putItem container item with
                        | Some (containerTriplets, (itemPut: ItemPut)) ->
                            let firstRes: StackItem list  =
                                [
                                    let newItems  = itemPut::itemPuts

                                    match containerTriplets with
                                    | [] -> yield ((remainingContainers @triedButNotFit |> mergeContainers), remainingItems, newItems)
                                    |  _ ->
                                    for triplet in containerTriplets do
                                        let rema =
                                            (remainingContainers @ triplet @ triedButNotFit) |> mergeContainers//|> List.tail //|> mergeContainers
                                       // checkConflictI newItems
                                        yield (rema
                                               |> List.sortBy (fun s -> (s.Coord.Z,  - (s.Dim |> dimToVolume))),
                                               remainingItems,
                                               newItems
                                               )
                                ]
                            //printfn "fjrstRes %A" firstRes
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
                        | _ ->loopContainers (remainingContainers, container :: triedButNotFit)
                    | _ -> []
                let sorted = (containerSet |> List.sortBy (fun s -> s.Coord.Z))

                let stackItems =
                    loopContainers (sorted , [])
                let totalStack = (stackItems@remainingStack)
                //printf "total stack %A" totalStack  //|> List.filter(fun (x,_,_) -> x.Length > 0)
                loop
                        (totalStack
                         |> List.sortBy (fun (x,y,z) ->
                            if x.Length = 0 then
                                Int32.MaxValue, Int32.MaxValue
                            else
                                ((x|> List.minBy (fun l -> l.Coord.Z)).Coord.Z),-((x|> List.maxBy (fun l -> l.Dim |> dimToVolume)).Dim |> dimToVolume)   )
                                 ,

                         counter - 1)
            | ( cs ,_, itemPuts)::_, _ -> Some (cs,itemPuts)
            | _, _ -> None
        loop ([ containers, items,[] ], 800)

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
open System.Diagnostics

let inline findUnfitItems itemsPut (items:Item list) =
    let itemsPutIds = itemsPut |> List.map(fun d -> d.Item.Id)
    items |> List.filter(fun i ->  itemsPutIds |> List.contains i.Id |> not)

let calcCost container items =
    match calculateCost (putItem container 3) [ container ] (items |> List.sortByDescending calcVolume) with
    | Some (cs,res) ->

        let unfitItems = findUnfitItems res items// printf "%A" unfitItems
        let maxZCoord =
            let max = (res |> List.maxBy (fun x -> x.Coord.Z + x.Item.Dim.Length))
            max.Item.Dim.Length + max.Coord.Z
        let sumZCoord =
            (res |> List.sumBy (fun x -> x.Coord.Z + x.Item.Dim.Length))
        float ((unfitItems |> List.sumBy calcVolume)*1000 + maxZCoord ) , res,cs
    | None _ -> Double.MaxValue, [],[]

type GlobalBest = { ItemsPut : ItemPut list; Cost : float; ContainerSet : Container list}

let rec calc (container: Container) (itemsWithCost: ItemsWithCost) (globalBest:GlobalBest) (T: float) (alpha:float) result (sw : Stopwatch)=
    if TMin >= T || sw.ElapsedMilliseconds > 30000L then
        printf "%A" sw.ElapsedMilliseconds
        globalBest
    else
        let items = itemsWithCost.Items

        let calculated, res , globalBest=
            if itemsWithCost.Cost = 0. then
                let cost, res,cs = calcCost container items
                { itemsWithCost with Cost = cost }, res, { ItemsPut = res ; Cost = cost; ContainerSet = cs }
            else
                itemsWithCost, result, globalBest

        let rec loop (itemsWC: ItemsWithCost, res) (globalBest:GlobalBest)  count =
            if count = 0 then
                itemsWC, res, globalBest
            else
                let items = itemsWC.Items
                let nbr = items |> mutate res |> mutate res |> mutate res |> mutate res |> mutate  res
                let nbrCost, nbrRes,cs = calcCost container nbr

                let nextItem, res , globalBest2=
                  //  printfn "costs: %f-%f" calculated.Cost nbrCost
                    if nbrCost < calculated.Cost then
                        { Items = nbr; Cost = nbrCost }, nbrRes,
                            if nbrCost < globalBest.Cost then
                                { ItemsPut = nbrRes ; Cost = nbrCost; ContainerSet =cs }
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
        calc container c globalBest (T * alpha) alpha r sw

let checkConflict (A:ItemPut) (B:ItemPut) =
    not (B.Coord.X >= A.Coord.X+A.Item.Dim.Width
        || B.Coord.X+B.Item.Dim.Width <= A.Coord.X
        || B.Coord.Y >= A.Coord.Y+A.Item.Dim.Height
        || B.Coord.Y+B.Item.Dim.Height <= A.Coord.Y
        || B.Coord.Z >= A.Coord.Z+A.Item.Dim.Length
        || B.Coord.Z+B.Item.Dim.Length <= A.Coord.Z)

let run  (container: Container) (items: Item list) (T: float) (alpha:float) =
    try
    let itemsWithCost = { Items = items |> List.map rotateToMinZ ; Cost = 0.}
    let globalBest = (calc container itemsWithCost { ItemsPut = []; Cost = Double.MaxValue; ContainerSet =[]} T alpha [] (Stopwatch.StartNew()))
    let itemsPut = globalBest.ItemsPut
    let volumeContainer = container.Dim.Height * container.Dim.Width * container.Dim.Length
    let itemsUnput = findUnfitItems itemsPut items
    let putVolume = itemsUnput |> List.sumBy calcVolume
    for item1 in itemsPut do
       for item2 in itemsPut do
           if item1.Item.Id <> item2.Item.Id && checkConflict item1 item2 then
                printfn "Items conflc %A %A" item1 item2
    printfn "%A" globalBest.ContainerSet
    printfn "afte m%A" (mergeContainers globalBest.ContainerSet)

    {   ItemsPut = itemsPut;
        ContainerVol = volumeContainer;
        ItemsUnput = itemsUnput;
        PutVolume = putVolume;
        Container = container

    }
    with e -> printfn "%A" e; reraise();
