module MyInput

open System
open Fable.Core.JsInterop

let init (el: obj) =
    el?addEventListener ("input",
                         (fun e ->

                             if Int32.Parse(el?value) > Int32.Parse(el?max)
                             then el?value <- el?max))
