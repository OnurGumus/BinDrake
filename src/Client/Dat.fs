module rec Dat
open Browser.Types
open Fable.Core
open System
[<ImportAll("dat.gui")>]
let exports : IExports = jsNative
type [<AllowNullLiteral>] IExports =
    abstract GUI: GUIStatic
    abstract GUIController: GUIControllerStatic
type [<AllowNullLiteral>] GUIParams =
    /// Handles GUI's element placement for you.
    abstract autoPlace: bool  with get, set
    /// If true, starts closed.
    abstract closed: bool  with get, set
    /// If true, close/open button shows on top of the GUI.
    abstract closeOnTop: bool  with get, set
    /// If true, GUI is closed by the "h" keypress.
    abstract hideable: bool  with get, set
    /// JSON object representing the saved state of this GUI.
    abstract load: obj  with get, set
    /// The name of this GUI.
    abstract name: string  with get, set
    /// The identifier for a set of saved values.
    abstract preset: string  with get, set
    /// The width of GUI element.
    abstract width: float  with get, set
type [<AllowNullLiteral>] GUI =
    abstract __controllers: ResizeArray<GUIController> with get, set
    abstract __folders: ResizeArray<GUI> with get, set
    abstract domElement: HTMLElement with get, set
    abstract add: target: Object * propName: string * ?min: float * ?max: float * ?step: float -> GUIController
    abstract add: target: Object * propName: string * status: bool -> GUIController
    abstract add: target: Object * propName: string * items: ResizeArray<string> -> GUIController
    abstract add: target: Object * propName: string * items: ResizeArray<float> -> GUIController
    abstract add: target: Object * propName: string * items: Object -> GUIController
    abstract addColor: target: Object * propName: string -> GUIController
    abstract remove: controller: GUIController -> unit
    abstract destroy: unit -> unit
    abstract addFolder: propName: string -> GUI
    abstract removeFolder: subFolder: GUI -> unit
    abstract ``open``: unit -> unit
    abstract close: unit -> unit
    abstract hide: unit -> unit
    abstract show: unit -> unit
    abstract remember: target: Object * [<ParamArray>] additionalTargets: ResizeArray<Object> -> unit
    abstract getRoot: unit -> GUI
    abstract getSaveObject: unit -> Object
    abstract save: unit -> unit
    abstract saveAs: presetName: string -> unit
    abstract revert: gui: GUI -> unit
    abstract listen: controller: GUIController -> unit
    abstract updateDisplay: unit -> unit
    abstract parent: GUI
    abstract scrollable: bool
    abstract autoPlace: bool
    abstract preset: string with get, set
    abstract width: float with get, set
    abstract name: string with get, set
    abstract closed: bool with get, set
    abstract load: Object
    abstract useLocalStorage: bool with get, set
type [<AllowNullLiteral>] GUIStatic =
    abstract CLASS_AUTO_PLACE: string with get, set
    abstract CLASS_AUTO_PLACE_CONTAINER: string with get, set
    abstract CLASS_MAIN: string with get, set
    abstract CLASS_CONTROLLER_ROW: string with get, set
    abstract CLASS_TOO_TALL: string with get, set
    abstract CLASS_CLOSED: string with get, set
    abstract CLASS_CLOSE_BUTTON: string with get, set
    abstract CLASS_CLOSE_TOP: string with get, set
    abstract CLASS_CLOSE_BOTTOM: string with get, set
    abstract CLASS_DRAG: string with get, set
    abstract DEFAULT_WIDTH: float with get, set
    abstract TEXT_CLOSED: string with get, set
    abstract TEXT_OPEN: string with get, set
    [<Emit "new $0($1...)">] abstract Create: ?option: GUIParams -> GUI
type [<AllowNullLiteral>] GUIController =
    abstract domElement: HTMLElement with get, set
    abstract ``object``: Object with get, set
    abstract property: string with get, set
    abstract options: option: obj option -> GUIController
    abstract name: name: string -> GUIController
    abstract listen: unit -> GUIController
    abstract remove: unit -> GUIController
    abstract onChange: fnc: (obj -> unit) -> GUIController
    abstract onFinishChange: fnc: (obj -> unit) -> GUIController
    abstract setValue: value: obj option -> GUIController
    abstract getValue: unit -> obj option
    abstract updateDisplay: unit -> GUIController
    abstract isModified: unit -> bool
    abstract min: n: float -> GUIController
    abstract max: n: float -> GUIController
    abstract step: n: float -> GUIController
    abstract fire: unit -> GUIController
type [<AllowNullLiteral>] GUIControllerStatic =
    [<Emit "new $0($1...)">] abstract Create: ``object``: Object * property: string -> GUIController