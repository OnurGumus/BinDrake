module Shared
open System
[<Struct>]
type Coordinates = { X: int; Y: int; Z: int }

[<Struct>]
type Dim =
    {
        Width: int
        Height: int
        Length: int
    }

type CalculationMode =
    | MinimizeLength
    | MinimizeHeight
    | MinimizeVolume

type ContainerMode =
    | SingleContainer
    | MultiContainer

type Container = { Dim: Dim; Coord: Coordinates; Weight : int }
type Item = { Dim: Dim; Weight : int; Id: string; Tag: string ; NoTop:bool; KeepTop:bool; KeepBottom : bool}
type ContainerTriplet = Container list
type ItemPut = { Item: Item; Coord: Coordinates }
type PutResult = ValueTuple<ContainerTriplet list ,  ItemPut ValueOption> ValueOption
[<Struct>]
type ItemsWithCost = { Items: Item list; Cost: float }
type PutItem = Container -> Item -> int -> PutResult
type StackItem = ValueTuple<Container list , Item list , ItemPut list>
[<Struct>]
type Counter = { Value : int }
type CalcResult = {
    ItemsPut : ItemPut list
    ContainerVol : int
    ItemsUnput : Item list
    PutVolume : int
    Container : Container
    EmptyContainers : Container list
}
[<Interface>]
type IStopwatch =
    abstract member ElapsedMilliseconds : Int64

[<Interface>]
type ILogger =
    abstract member LogError : Exception -> unit
    abstract member Log : string -> arr : obj array -> unit

module ClientModel =
    type Calculation =
        | NotCalculated
        | Calculating
        | Calculated of CalcResult list

    type RowItem =
        {
            Width: int
            Height: int
            Length: int
            Weight: int
            Color: string
            Quantity: int
            Stackable: bool
            KeepTop: bool
            KeepBottom: bool
        }

    type ContainerItem =
        {
            Width: int
            Height: int
            Length: int
            Weight: int
        }
    type Model =
        {
            Calculation: Calculation
            CalculationMode: CalculationMode
            ContainerMode: ContainerMode
            Container: Container option
            ContainerItem: ContainerItem option
            RowItems: (RowItem option * string) list
            TotalVolume: int option
            CurrentResultIndex: int
            UrlShown : bool
            Loading : bool
        }

type Calcs = { ContainerMode : ContainerMode; CalculationMode : CalculationMode}
module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

/// A type that specifies the communication protocol between client and server
/// to learn more, read the docs at https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type ICalcApi =
    {
        run  : Calcs -> Container -> Item list -> float -> float -> Async<CalcResult list>
        saveModel  : ClientModel.Model -> Async<Guid>
        loadModel  : Guid -> Async<ClientModel.Model>
    }

