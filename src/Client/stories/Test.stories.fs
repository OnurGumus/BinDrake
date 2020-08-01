module Test
open Fable.Core.JsInterop
open Feliz

importAll "./MyInput.js"
exportDefault (createObj ["title" ==>"Test"])
let Summary () = Html.input[ prop.type' "text" ; prop.custom("is", "my-input"); prop.custom("max",25); prop.custom("min",0)]
