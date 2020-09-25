module BinPacker

open Shared
open System

let random = System.Random()

module Rotate =
    let inline rotateZ (item: Item) =
        if item.KeepTop then
            item
        else

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
        if item.KeepTop then
            item
        else
            { item with
                Dim =
                    {
                        Height = item.Dim.Length
                        Width = item.Dim.Width
                        Length = item.Dim.Height
                    }
            }

    let rotateToMinZ calculationMode item =
        let comb =
            let x = item |> rotateX
            let y = item |> rotateY
            let z = item |> rotateZ
            [ item; x; y; z ]

        match calculationMode with
        | MinimizeHeight -> comb |> List.minBy (fun d -> d.Dim.Height)
        | MinimizeLength -> comb |> List.minBy (fun d -> d.Dim.Length)


    let inline randomRotate item: Item =
        let r = random.NextDouble()
        if r < 0.333 then rotateZ item
        elif r < 0.666 then rotateY item
        else rotateX item



let dimToVolume (d: Dim) = d.Width * d.Height * d.Length

let dimToArea (d: Dim) =
    d.Width
    * d.Height
    + (d.Length * d.Width)
    + (d.Length * d.Height)

module Conflict =
    let checkConflictC (A: Container) (B: Container) =
        not
            (B.Coord.X
             >= A.Coord.X
             + A.Dim.Width
             || B.Coord.X + B.Dim.Width <= A.Coord.X
             || B.Coord.Y >= A.Coord.Y + A.Dim.Height
             || B.Coord.Y + B.Dim.Height <= A.Coord.Y
             || B.Coord.Z >= A.Coord.Z + A.Dim.Length
             || B.Coord.Z + B.Dim.Length <= A.Coord.Z)

    let containerssCheck (containers: Container list) =
        for item1 in containers do
            for item2 in containers do
                if item1 <> item2 && checkConflictC item1 item2 then
                    printfn "contianer conflc %A %A" item1 item2
                    failwith "conf"

    let checkConflict (A: ItemPut) (B: ItemPut) =
        not
            (B.Coord.X
             >= A.Coord.X
             + A.Item.Dim.Width
             || B.Coord.X + B.Item.Dim.Width <= A.Coord.X
             || B.Coord.Y >= A.Coord.Y + A.Item.Dim.Height
             || B.Coord.Y + B.Item.Dim.Height <= A.Coord.Y
             || B.Coord.Z >= A.Coord.Z + A.Item.Dim.Length
             || B.Coord.Z + B.Item.Dim.Length <= A.Coord.Z)

let mergeContainers (containers: Container list) =
    let containers = containers |> List.indexed

    let mergersZ containers =
        seq {
            for (i1, c1: Container) in containers do
                for (i2, c2) in containers do
                    if (i1 <> i2) then
                        if c1.Coord.X = c2.Coord.X
                           && c1.Coord.Y = c2.Coord.Y
                           && c1.Dim.Width = c2.Dim.Width
                           && c2.Dim.Height = c1.Dim.Height
                           && c2.Coord.Z = c1.Coord.Z + c1.Dim.Length then
                            ((i1, c1), (i2, c2))
        }



    let mergeContainersZ (c1: Container) (c2: Container) =
        {
            Dim =
                {
                    Width = c1.Dim.Width
                    Height = c1.Dim.Height
                    Length = c1.Dim.Length + c2.Dim.Length
                }
            Weight = c1.Weight + c2.Weight
            Coord = c1.Coord
        }

    let mergersY containers =
        seq {
            for (i1, c1: Container) in containers do
                for (i2, c2) in containers do
                    if (i1 <> i2) then
                        if c1.Coord.X = c2.Coord.X
                           && c1.Coord.Z = c2.Coord.Z
                           && c1.Dim.Length = c2.Dim.Length
                           && c2.Dim.Width = c1.Dim.Width
                           && c2.Coord.Y = c1.Coord.Y + c1.Dim.Height then
                            ((i1, c1), (i2, c2))
        }



    let mergeContainersY (c1: Container) (c2: Container) =
        {
            Dim =
                {
                    Length = c1.Dim.Length
                    Width = c1.Dim.Width
                    Height = c1.Dim.Height + c2.Dim.Height
                }
            Weight = c1.Weight + c2.Weight
            Coord = c1.Coord
        }

    let mergersX containers =
        seq {
            for (i1, c1: Container) in containers do
                for (i2, c2) in containers do
                    if (i1 <> i2) then
                        if c1.Coord.Y = c2.Coord.Y
                           && c1.Coord.Z = c2.Coord.Z
                           && c1.Dim.Length = c2.Dim.Length
                           && c2.Dim.Height = c1.Dim.Height
                           && c2.Coord.X = c1.Coord.X + c1.Dim.Width then
                            ((i1, c1), (i2, c2))
        }



    let mergeContainersX (c1: Container) (c2: Container) =
        {
            Dim =
                {
                    Length = c1.Dim.Length
                    Height = c1.Dim.Height
                    Width = c1.Dim.Width + c2.Dim.Width
                }
            Weight = c1.Weight + c2.Weight
            Coord = c1.Coord
        }

    let removeAt index1 index2 input =
        input
        |> List.mapi (fun i el -> (i <> index1 && i <> index2, el))
        |> List.filter fst
        |> List.map snd

    let rec containerLoop merger containers mergeContainersf mergersf added =
        if merger |> Seq.isEmpty then
            containers @ (added |> List.indexed)
        else
            let ((i1, c1), (i2, c2)) = merger |> Seq.head
            let newC = mergeContainersf c1 c2
            let prev = containers.Length
            let containers = containers |> removeAt i1 i2

            let containers =
                containers |> List.map snd |> List.indexed

            let mergers = mergersf containers
            containerLoop mergers ((containers)) mergeContainersf mergersf (newC :: added)

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

    //containers |> Conflict.containerssCheck

    containers

let containerNestedSort (calcMode: CalculationMode) (containers: Container list) =
    match calcMode with
    | MinimizeLength ->
        let targetContainer =
            (containers
             |> List.minBy (fun x -> (float x.Coord.Z, -(x.Dim |> dimToVolume))))

        (float targetContainer.Coord.Z, -(targetContainer.Dim |> dimToVolume))
    | MinimizeHeight ->
        let targetContainer =
            (containers
             |> List.minBy (fun x -> (float x.Coord.Y, -(x.Dim |> dimToVolume))))

        (float targetContainer.Coord.Y, -(targetContainer.Dim |> dimToVolume))


let rec putItem (rootContainer: Container) (calculationMode: CalculationMode) tryCount: PutItem =
    fun container item (weightPut: int) ->
        let remainingWidth = container.Dim.Width - item.Dim.Width
        let remainingHeight = container.Dim.Height - item.Dim.Height
        let remainingLength = container.Dim.Length - item.Dim.Length

        let remainingWeight =
            rootContainer.Weight - weightPut - item.Weight

        if remainingWeight < 0 then
            ValueSome([ [ container ] ], ValueNone)
        elif item.NoTop
             && container.Coord.Y
             + container.Dim.Height < rootContainer.Dim.Height then
            ValueNone
        elif item.KeepBottom && container.Coord.Y > 0 then
            ValueNone
        elif (remainingHeight < 0)
             || remainingLength < 0
             || remainingWidth < 0 then
            if tryCount > 0 then
                if tryCount = 3 then
                    let item = item |> Rotate.rotateZ

                    putItem rootContainer calculationMode (tryCount - 1) container item weightPut
                else if tryCount = 2 then
                    let item = item |> Rotate.rotateZ |> Rotate.rotateY

                    putItem rootContainer calculationMode (tryCount - 1) container item weightPut
                else
                    let item = item |> Rotate.rotateY |> Rotate.rotateX

                    putItem rootContainer calculationMode (tryCount - 1) container item weightPut

            else
                ValueNone
        else
            let calculationMode =
                if item.NoTop then CalculationMode.MinimizeHeight else calculationMode

            let containerSort =
                match calculationMode with
                | MinimizeHeight -> (fun (s: Container) -> s.Coord.Y)
                | MinimizeLength -> (fun s -> s.Coord.Z)

            let noTopFilter (s: Container) =
                (item.NoTop
                 && s.Coord.Y = container.Coord.Y
                 + item.Dim.Height
                 && s.Coord.X = container.Coord.X
                 && s.Coord.Z = container.Coord.Z)
                |> not

            let config1, config2, config3, config4 =
                match calculationMode with
                | CalculationMode.MinimizeLength ->
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
                                Weight = 0
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
                                Weight = 0
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

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
                                Weight = 0
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
                                Weight = 0
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

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
                                Weight = 0
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
                                Weight = 0
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

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
                                Weight = 0
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
                                Weight = 0
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

                    config1, config2, config3, config4
                | _ ->
                    let config1 =
                        let topBlock =
                            {
                                Dim =
                                    {
                                        Width = container.Dim.Width
                                        Height = item.Dim.Height
                                        Length = remainingLength
                                    }
                                Coord =
                                    {
                                        X = container.Coord.X
                                        Y = container.Coord.Y
                                        Z = container.Coord.Z + item.Dim.Length
                                    }
                                Weight = 0
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
                                Weight = 0
                            }

                        let remainingBlock =
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

                    let config2 =

                        let topBlock =
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
                                Weight = 0
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
                                Weight = 0
                            }

                        let remainingBlock =
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

                    let config3 =
                        let topBlock =
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
                                Weight = 0
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
                                Weight = 0
                            }

                        let remainingBlock =
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

                    let config4 =
                        let topBlock =
                            {
                                Dim =
                                    {
                                        Width = item.Dim.Width
                                        Height = container.Dim.Height
                                        Length = remainingLength
                                    }
                                Coord =
                                    {
                                        X = container.Coord.X
                                        Y = container.Coord.Y
                                        Z = container.Coord.Z + item.Dim.Length
                                    }
                                Weight = 0
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
                                Weight = 0
                            }

                        let remainingBlock =
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
                                Weight = 0
                            }

                        [ topBlock; sideBlock; remainingBlock ]
                        |> List.filter (fun s ->
                            s.Dim.Width > 0
                            && s.Dim.Height > 0
                            && s.Dim.Length > 0)
                        |> List.filter noTopFilter
                        |> List.sortBy containerSort

                    config1, config2, config3, config4


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

            let all =
                match calculationMode with
                | MinimizeLength ->
                    [ config2; config1; config3; config4 ]
                    |> List.distinct
                | MinimizeHeight ->
                    [ config3; config4; config2; config1 ]
                    |> List.distinct

            let t1 =
                all |> List.item (random.Next(0, all.Length))

            let t2 =
                all
                |> function
                | [] -> []
                | [ s ] -> s
                | _ ->
                    all
                    |> List.filter (fun e -> e <> t1)
                    |> fun l -> List.item (random.Next(0, l.Length)) l

            let sets =
                if item.NoTop then
                    match calculationMode with
                    | CalculationMode.MinimizeLength -> [ config2; config1 ]
                    | _ -> [ config3; config4 ]
                else
                    [ t1; t2 ]

            let res =
                ValueSome
                    (struct (all
                             |> List.distinct
                             |> List.filter (fun f -> f |> List.isEmpty |> not)
                             |> List.sortBy (containerNestedSort calculationMode),
                             ValueSome itemPut))

            res

let checkConflictI itemsPut =
    let checkConflict (A: ItemPut) (B: ItemPut) =
        not
            (B.Coord.X
             >= A.Coord.X
             + A.Item.Dim.Width
             || B.Coord.X + B.Item.Dim.Width <= A.Coord.X
             || B.Coord.Y >= A.Coord.Y + A.Item.Dim.Height
             || B.Coord.Y + B.Item.Dim.Height <= A.Coord.Y
             || B.Coord.Z >= A.Coord.Z + A.Item.Dim.Length
             || B.Coord.Z + B.Item.Dim.Length <= A.Coord.Z)

    for item1 in itemsPut do
        for item2 in itemsPut do
            if item1.Item.Id
               <> item2.Item.Id
               && checkConflict item1 item2 then
                printfn "Items conflc %A %A" item1 item2
                failwith "wr"

let calculateCost (calculationMode: CalculationMode) =
    fun putItem containers items ->
        let rec loop =
            function
            | struct (containerSet, [], itemPuts) :: _, _ -> ValueSome(containerSet, itemPuts)
            | struct (containerSet, item :: remainingItems, itemPuts) :: remainingStack, counter when counter > 0 ->
                let containerSet = containerSet |> mergeContainers

                let rec loopContainers: (struct (Container list * Container list)) -> StackItem list =
                    function
                    | (container :: remainingContainers) as cs, triedButNotFit ->
                        // let item =
                        //  if remainingItems.Length < 8 then
                        //     Rotate.rotateToMinZ calculationMode item
                        //  else
                        //     item
                        match (putItem container item (itemPuts |> List.sumBy (fun x -> x.Item.Weight))) with
                        | ValueSome (struct (containerTriplets, (itemPut: ItemPut ValueOption))) ->
                            let firstRes: StackItem list =
                                [
                                    let newItems =
                                        match itemPut with
                                        | ValueNone -> itemPuts
                                        | ValueSome i -> i :: itemPuts


                                    match containerTriplets with
                                    | [] ->
                                        yield
                                            ((remainingContainers
                                              @ triedButNotFit
                                              |> mergeContainers),
                                             remainingItems,
                                             newItems)
                                    | _ ->
                                        for triplet in containerTriplets do
                                            let rema =
                                                (remainingContainers @ triplet @ triedButNotFit)
                                                |> mergeContainers //|> List.tail //|> mergeContainers
                                            // checkConflictI newItems
                                            let containerSort =
                                                match calculationMode with
                                                | MinimizeHeight ->
                                                    (fun (s: Container) -> (s.Coord.Y, -(s.Dim |> dimToVolume)))
                                                | MinimizeLength -> (fun s -> (s.Coord.Z, -(s.Dim |> dimToVolume)))

                                            yield (rema |> List.sortBy containerSort, remainingItems, newItems)
                                ]

                            firstRes

                        | _ -> loopContainers (remainingContainers, container :: triedButNotFit)
                    | _ -> []

                let containerSort =
                    match calculationMode with
                    | MinimizeHeight -> (fun (s: Container) -> s.Coord.Y)
                    | MinimizeLength -> (fun s -> s.Coord.Z)

                let sorted =
                    (containerSet |> List.sortBy containerSort)

                let stackItems = loopContainers (sorted, [])
                let totalStack = (stackItems @ remainingStack)
                loop
                    (totalStack,
                     //  |> List.sortBy (fun (struct (x, _, _)) ->
                     //      if x.Length = 0 then
                     //          struct (Int32.MaxValue, Int32.MaxValue)
                     //      else
                     //         let containerSort containers  =
                     //             match calculationMode with
                     //             | MinimizeHeight ->  (containers |> containerNestedSort)
                     //             | MinimizeLength -> (containers |> containerNestedSort)

                     //         let z = (x |> List.sortBy containerSort)
                     //         ),

                     counter - 1)
            | (cs, _, itemPuts) :: _, _ -> ValueSome(cs, itemPuts)
            | _, _ -> ValueSome(containers, [])

        loop ([ containers, items, [] ], 800)

let calcVolume (item: Item) =
    float (item.Dim.Width)
    * float (item.Dim.Height)
    * float (item.Dim.Length)

let maxDim (item: Item) =
    Math.Max(item.Dim.Width, Math.Max(item.Dim.Height, item.Dim.Length))

let maxVol (item: Item) =
    item.Dim.Width * item.Dim.Height * item.Dim.Length



let inline mutate (calcMode: CalculationMode) (itemsPut: ItemPut list) (items: Item list) =
    if items |> List.isEmpty then
        []
    else
        let firstSwap =
            if random.NextDouble() > 0.6
               || itemsPut |> List.isEmpty then
                random.Next(0, items.Length)
            else
                let max =
                    match calcMode with
                    | MinimizeLength -> (fun x -> x.Coord.Z)
                    | MinimizeHeight -> (fun x -> x.Coord.Y)

                let maxId = itemsPut |> List.maxBy max

                items
                |> List.findIndex (fun x -> x.Id = maxId.Item.Id)

        let secondSwap = random.Next(0, items.Length)
        let arr = items |> Array.ofList
        let tmp = arr.[firstSwap] |> Rotate.randomRotate
        arr.[firstSwap] <- arr.[secondSwap]
        arr.[secondSwap] <- tmp
        arr |> List.ofArray

let TMin = 0.01
open System
open System.Diagnostics

let inline findUnfitItems itemsPut (items: Item list) =
    let itemsPutIds =
        itemsPut |> List.map (fun d -> d.Item.Id)

    items
    |> List.filter (fun i -> itemsPutIds |> List.contains i.Id |> not)

let calcCost rootContainer (calculationMode: CalculationMode) containers items =
    match calculateCost
              calculationMode
              (putItem rootContainer calculationMode 3)
              containers
              (items |> List.sortByDescending calcVolume) with
    | ValueSome (cs, res) ->
        let cs = (cs |> mergeContainers)
        let unfitItems = findUnfitItems res items

        let sumZ =
            if res.Length = 0 then
                Int32.MaxValue
            else
                match calculationMode with
                | MinimizeHeight ->
                    (res
                     |> List.sumBy (fun x -> x.Coord.Y + x.Item.Dim.Height))
                | MinimizeLength ->
                    (res
                     |> List.sumBy (fun x -> x.Coord.Z + x.Item.Dim.Length))

        let maxZCoord =
            if res.Length = 0 then
                Int32.MaxValue
            else
                match calculationMode with
                | MinimizeHeight ->
                    (res
                     |> List.maxBy (fun x -> x.Coord.Y + x.Item.Dim.Height))
                    |> fun max -> max.Item.Dim.Height + max.Coord.Y

                | MinimizeLength ->
                    (res
                     |> List.maxBy (fun x -> x.Coord.Z + x.Item.Dim.Length))
                    |> fun max -> max.Item.Dim.Length + max.Coord.Z



        float
            ((unfitItems |> List.sumBy calcVolume)
             * 1000.
             // + 1000. * float (cs |> List.sumBy(fun x->x.Dim |> dimToArea))
             + 1. * float (sumZ)
             + 1.0 * float (maxZCoord)),
        res,
        cs
    | ValueNone _ -> Double.MaxValue, [], []

type GlobalBest =
    {
        ItemsPut: ItemPut list
        Cost: float
        ContainerSet: Container list
    }

let rec calc (rootContainer: Container)
             (calculationMode: CalculationMode)
             (containers: Container list)
             (itemsWithCost: ItemsWithCost)
             (globalBest: GlobalBest)
             (T: float)
             (alpha: float)
             result
             (sw: Stopwatch)
             =
    if TMin
       >= T
       || sw.ElapsedMilliseconds > 400L
       || (itemsWithCost.Items.Length = 1
           && globalBest.ItemsPut.Length = 1) then
        globalBest
    else
        let items = itemsWithCost.Items

        let calculated, res, globalBest =
            if itemsWithCost.Cost = 0. then
                let cost, res, cs =
                    calcCost rootContainer calculationMode containers items

                { itemsWithCost with Cost = cost },
                res,
                {
                    ItemsPut = res
                    Cost = cost
                    ContainerSet = cs
                }
            else
                itemsWithCost, result, globalBest

        let rec loop (itemsWC: ItemsWithCost, res) (globalBest: GlobalBest) count =
            if count = 0 then
                itemsWC, res, globalBest
            else
                let items = itemsWC.Items
                let mutate = mutate calculationMode

                let nbr =
                    items
                    |> mutate res
                    |> mutate res
                    |> mutate res
                    |> mutate res
                    |> mutate res

                let nbrCost, nbrRes, cs =
                    calcCost rootContainer calculationMode containers nbr

                let nextItem, res, globalBest2 =
                    //printfn "costs: %f-%f" calculated.Cost nbrCost
                    if nbrCost < calculated.Cost then
                        { Items = nbr; Cost = nbrCost },
                        nbrRes,
                        (if nbrCost < globalBest.Cost then
                            {
                                ItemsPut = nbrRes
                                Cost = nbrCost
                                ContainerSet = cs
                            }
                         else
                             globalBest)
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
                            res,
                            globalBest

                loop (nextItem, res) globalBest2 (count - 1)

        if TMin
           >= T
           || sw.ElapsedMilliseconds > 400L
           || (itemsWithCost.Items.Length = 1
               && globalBest.ItemsPut.Length = 1) then
            globalBest
        else
            let c, r, globalBest = loop (calculated, res) globalBest 1
            calc rootContainer calculationMode containers c globalBest (T * alpha) alpha r sw


let runInner (rootContainer: Container)
             (calculationMode: CalculationMode)
             (containers: Container list)
             (items: Item list)
             (T: float)
             (alpha: float)
             =
    try
        let itemsWithCost =
            {
                Items = items // |> List.map Rotate.rotateToMinZ
                Cost = 0.
            }

        let globalBest =
            (calc
                rootContainer
                 calculationMode
                 containers
                 itemsWithCost
                 {
                     ItemsPut = []
                     Cost = Double.MaxValue
                     ContainerSet = containers
                 }
                 T
                 alpha
                 []
                 (Stopwatch.StartNew()))

        let itemsPut = globalBest.ItemsPut

        let volumeContainer =
            rootContainer.Dim.Height
            * rootContainer.Dim.Width
            * rootContainer.Dim.Length

        let itemsUnput = findUnfitItems itemsPut items
        let putVolume = itemsUnput |> List.sumBy calcVolume
        for item1 in itemsPut do
            for item2 in itemsPut do
                if item1.Item.Id
                   <> item2.Item.Id
                   && Conflict.checkConflict item1 item2 then
                    printfn "Items conflc %A %A" item1 item2

        let res =
            {
                ItemsPut = itemsPut
                ContainerVol = volumeContainer
                ItemsUnput = itemsUnput
                PutVolume = int (putVolume)
                Container = rootContainer
                EmptyContainers = globalBest.ContainerSet
            }

        res
    with e ->
        printfn "%A" e
        reraise ()

let swap (a: _ []) x y =
    let tmp = a.[x]
    a.[x] <- a.[y]
    a.[y] <- tmp

// shuffle an array (in-place)
let shuffle a =
    Array.iteri (fun i _ -> swap a i (random.Next(i, Array.length a))) a
    a

let defaultBatchSize = 20

let runPerContainer (rootContainer: Container)
                    (calculationMode: CalculationMode)
                    (items: Item list)
                    (T: float)
                    (alpha: float)
                    =
    let sw = Stopwatch.StartNew()

    let rec loop (rootContainer: Container)
                 (containers: Container list)
                 (items: Item list)
                 (results: CalcResult list)
                 batchCount
                 retryCount
                 =
        let containerSort =
            match calculationMode with
            | MinimizeHeight -> (fun (c: Container) -> ((c.Coord.Y), -(c.Dim |> dimToVolume)))
            | MinimizeLength -> (fun c -> ((c.Coord.Z), -(c.Dim |> dimToVolume)))

        let containers = containers |> List.sortBy containerSort
        let oldBatchCount = batchCount

        let batchCount =
            if (items.Length > 0 && items.Head.NoTop) then 1 else batchCount

        let currentItems, remainingItems =
            if items.Length > batchCount then items |> List.splitAt batchCount else items, []

        let batchCount = oldBatchCount

        let res =
            runInner rootContainer calculationMode containers currentItems T alpha

        let rootContainer =
            { rootContainer with
                Weight =
                    rootContainer.Weight
                    - (res.ItemsPut
                       |> List.sumBy (fun s -> s.Item.Weight))
            }

        let lastunput = res.ItemsUnput

        let newItems =
            remainingItems
            @ (lastunput
               |> Array.ofList
               |> shuffle
               |> List.ofArray)

        let res = { res with ItemsUnput = [] }
        let results = res :: results

        let retryCount =
            if res.ItemsPut.IsEmpty then retryCount - 1 else retryCount

        Serilog.Log.Information
            ("unput items :{@unput} - remaining:{@remaining} -batchCount:{@batchCount}",
             lastunput.Length,
             remainingItems.Length,
             batchCount)

        let rbatchCount = Math.Max(1, (batchCount / 2))

        let timeOut =
            sw.Elapsed.TotalSeconds > 90.
            || items
               |> List.forall (fun x -> x.Weight > rootContainer.Weight)
        //printfn "rbatchCount %i" rbatchCount
        match timeOut, newItems, retryCount with
        | false, _ :: _, 1
        | false, _ :: _, 2
        | false, _ :: _, 3 ->
            loop rootContainer (res.EmptyContainers |> mergeContainers) newItems results rbatchCount retryCount
        | true, _, _
        | _, _, 0
        | _, _, -1
        | _, [], _ ->
            let itemsPut =
                results
                |> List.map (fun c -> c.ItemsPut)
                |> List.fold (@) []
                |> List.distinct

            let res =
                {
                    Container = rootContainer
                    ContainerVol = rootContainer.Dim |> dimToVolume
                    EmptyContainers = res.EmptyContainers
                    ItemsPut = itemsPut
                    ItemsUnput = lastunput @ remainingItems
                    PutVolume =
                        itemsPut
                        |> List.map (fun x -> x.Item.Dim |> dimToVolume)
                        |> function
                        | [] -> 0
                        | other -> other |> List.sum
                }

            res

        | _ -> loop rootContainer (res.EmptyContainers |> mergeContainers) newItems results batchCount retryCount

    let rec outerLoop (items: Item list) retryCount resList =

        let defaultBatchSize = if items.Length < 100 then 15 else 20

        let res =
            loop rootContainer [ rootContainer ] (items) [] defaultBatchSize 6

        let res =
            match res.ItemsUnput with
            | [] ->
                let newRootContainer, max =
                    match calculationMode with
                    | MinimizeLength ->
                        let maxLength =
                            res.ItemsPut
                            |> List.map (fun x -> x.Item.Dim.Length + x.Coord.Z)
                            |> List.max

                        { rootContainer with
                            Dim =
                                { rootContainer.Dim with
                                    Length = maxLength
                                }
                        },
                        maxLength
                    | MinimizeHeight ->
                        let maxHeight =
                            res.ItemsPut
                            |> List.map (fun x -> x.Item.Dim.Height + x.Coord.Y)
                            |> List.max

                        { rootContainer with
                            Dim =
                                { rootContainer.Dim with
                                    Length = maxHeight
                                }
                        },
                        maxHeight

                let newRes =
                    loop newRootContainer [ rootContainer ] (items) [] defaultBatchSize 6

                match newRes.ItemsUnput with
                | [] when max > 0 ->
                    let newContainer =
                        match calculationMode with
                        | MinimizeLength ->
                            ({
                                 Dim =
                                     {
                                         Width = rootContainer.Dim.Width
                                         Height = rootContainer.Dim.Height
                                         Length = rootContainer.Dim.Length - max
                                     }
                                 Coord = { X = 0; Y = 0; Z = max }
                                 Weight = 0
                             }: Container)
                        | CalculationMode.MinimizeHeight ->
                             ({
                                  Dim =
                                      {
                                          Width = rootContainer.Dim.Width
                                          Height = rootContainer.Dim.Height - max
                                          Length = rootContainer.Dim.Length
                                      }
                                  Coord = { X = 0; Y = max; Z = 0 }
                                  Weight = 0
                              }: Container)

                    { newRes with
                        Container = rootContainer
                        ContainerVol = rootContainer.Dim |> dimToVolume
                        EmptyContainers =
                            newContainer
                            :: res.EmptyContainers
                            |> mergeContainers
                    }
                | _ -> res
            | _ -> res

        let timeOut = sw.Elapsed.TotalSeconds > 90.
        let results = res :: resList
        match timeOut, res.ItemsUnput, retryCount with
        | true, _, _
        | _, _, 0 -> results
        | _, [], _ -> results
        | _, _, _ ->
            let rec loopMutate items =
                function
                | 0 -> items
                | n -> loopMutate (items |> mutate calculationMode res.ItemsPut) (n - 1)

            outerLoop (loopMutate items (items.Length / 10)) (retryCount - 1) results

    try
        let resList =
            outerLoop
                (items
                 |> List.map (Rotate.rotateToMinZ calculationMode)
                 |> List.sortByDescending (fun x -> (x.KeepBottom, maxDim x)))
                10
                []

        let res =
            resList |> List.maxBy (fun x -> x.PutVolume)

        Serilog.Log.Information("Result {@res}", res)

        let convertContainerToItemPut (container: Container): ItemPut =
            {
                Coord = container.Coord
                Item =
                    {
                        Dim = container.Dim
                        Id = Guid.NewGuid().ToString()
                        Tag = sprintf "rgb(%i,%i,%i)" (random.Next(256)) (random.Next(256)) (random.Next(256))
                        NoTop = false
                        KeepTop = false
                        KeepBottom = false
                        Weight = 0
                    }
            }
        //let res = {res with ItemsPut = res.EmptyContainers |> List.map convertContainerToItemPut}

        res
    with e ->
        Serilog.Log.Error(e, "error")
        reraise ()

let run (rootContainer: Container)
        (containerMode: ContainerMode)
        (calculationMode: CalculationMode)
        (items: Item list)
        (T: float)
        (alpha: float)
        =
    let rec loop results unputItems =
        let res =
            runPerContainer rootContainer calculationMode unputItems T alpha

        let res =
            match res.ItemsUnput with
            | [] ->
                let newRootContainer, max =
                    match calculationMode with
                    | MinimizeLength ->
                        let maxLength =
                            res.ItemsPut
                            |> List.map (fun x -> x.Item.Dim.Length + x.Coord.Z)
                            |> List.max

                        { rootContainer with
                            Dim =
                                { rootContainer.Dim with
                                    Length = maxLength
                                }
                        },
                        maxLength
                    | MinimizeHeight ->
                        let maxHeight =
                            res.ItemsPut
                            |> List.map (fun x -> x.Item.Dim.Height + x.Coord.Y)
                            |> List.max

                        { rootContainer with
                            Dim =
                                { rootContainer.Dim with
                                    Length = maxHeight
                                }
                        },
                        maxHeight

                if max = 0 then
                    res
                else
                    let newRes =
                        runPerContainer newRootContainer calculationMode unputItems T alpha

                    match newRes.ItemsUnput with
                    | [] ->
                        { newRes with
                            Container = rootContainer
                            ContainerVol = rootContainer.Dim |> dimToVolume
                        }
                    | _ -> res
            | _ -> res

        let sumRes = res :: results
        printf "%A" containerMode
        match containerMode with
        | SingleContainer -> sumRes
        | _ ->
            if sumRes.Length > 20 then
                sumRes
            else
                match res.ItemsUnput with
                | [] -> sumRes
                | unput when unput.Length = unputItems.Length -> sumRes
                | _ -> loop sumRes res.ItemsUnput

    (loop [] items) |> List.rev
