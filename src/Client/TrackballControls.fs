// ts2fable 0.7.1
module rec TrackballControls

open System
open Fable.Core
open Fable.Core.JS
open Browser.Types

module THREE = Three

[<ImportAll("three-trackballcontrols")>]
let TrackballControls: TrackballControlsStatic = jsNative

[<AllowNullLiteral>]
type IExports =
    abstract TrackballControls: TrackballControlsStatic

[<AllowNullLiteral>]
type TrackballControls =
    inherit THREE.EventDispatcher
    abstract object: THREE.Camera with get, set
    abstract domElement: HTMLElement with get, set
    abstract window: Window with get, set
    abstract enabled: bool with get, set
    abstract screen: obj option with get, set
    abstract rotateSpeed: float with get, set
    abstract zoomSpeed: float with get, set
    abstract panSpeed: float with get, set
    abstract noRotate: bool with get, set
    abstract noZoom: bool with get, set
    abstract noPan: bool with get, set
    abstract staticMoving: bool with get, set
    abstract dynamicDampingFactor: float with get, set
    abstract minDistance: float with get, set
    abstract maxDistance: float with get, set
    abstract keys: ResizeArray<float> with get, set
    abstract target: THREE.Vector3 with get, set
    abstract dispose: unit -> unit
    abstract handleResize: unit -> unit
    abstract getMouseOnScreen: obj option with get, set
    abstract getMouseOnCircle: obj option with get, set
    abstract rotateCamera: (unit -> unit) with get, set
    abstract zoomCamera: (unit -> unit) with get, set
    abstract panCamera: (unit -> unit) with get, set
    abstract checkDistances: unit -> unit
    abstract update: obj -> unit
    abstract reset: unit -> unit

[<AllowNullLiteral>]
type TrackballControlsStatic =
    [<Emit "new $0($1...)">]
    abstract Create: object:THREE.Camera * domElement:HTMLElement * ?domWindow:Window -> TrackballControls
