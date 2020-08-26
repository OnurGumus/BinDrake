namespace Shared
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
[<Struct>]
type Container = { Dim: Dim; Coord: Coordinates }
[<Struct>]
type Item = { Dim: Dim; Id: string; Tag: string ; NoTop:bool}
type ContainerTriplet = Container list
[<Struct>]
type ItemPut = { Item: Item; Coord: Coordinates }
type PutResult = (ContainerTriplet list * ItemPut) option
[<Struct>]
type ItemsWithCost = { Items: Item list; Cost: float }
type PutItem = Container -> Item -> PutResult
type StackItem = Container list * Item list * ItemPut list
type Counter = { Value : int }
type CalcResult = {
    ItemsPut : ItemPut list
    ContainerVol : int
    ItemsUnput : Item list
    PutVolume : int
    Container : Container
}

module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

/// A type that specifies the communication protocol between client and server
/// to learn more, read the docs at https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type ICounterApi =
    {
        initialCounter : unit -> Async<Counter>
        run  : Container -> Item list -> float -> float -> Async<CalcResult>
    }

