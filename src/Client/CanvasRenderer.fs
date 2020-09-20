module CanvasRenderer

open Fable.Core
open Browser.Dom
open Fable.Core.JsInterop
open Fable.Core.JS
open System
open Shared

let mutable demoMode2 = false

let  gui = Dat.exports.GUI.Create();

let THREE = Three.exports
let scene = THREE.Scene.Create()

let camera =
    THREE.PerspectiveCamera.Create(30., window.innerWidth / window.innerHeight, 20., 10000.)

let opt =
    jsOptions<Three.WebGLRendererParameters> (fun x ->
        x.antialias <- Some true
        x.canvas <- Some(!^(document.getElementById ("myCanvas"))))

let renderer = THREE.WebGLRenderer.Create(opt)

renderer.setClearColor (!^THREE.Color.Create(!^(float 0xFFFFFF)))
renderer.setSize (window.innerWidth, window.innerHeight)
renderer.shadowMap?enabled <- true

let axes = THREE.AxesHelper.Create(20.)
let currentContainer = ResizeArray<Three.Object3D>()
let mutable lastLController = None
let mutable lastHController = None

let renderPlane (container: Container) =

    let x =
        (System.Math.Max
            (1,
             System.Math.Min
                 (40,
                  50000
                  / (container.Dim.Width * container.Dim.Length)))
         |> float)
        / 1.5

    let x =
        if container.Dim.Width + container.Dim.Length > 1000
        then x / 2.5
        else x / 1.5

    camera.position.set (500. / x, 550. / x, -700. / x)
    |> ignore
    scene.remove currentContainer |> ignore

    let planeGeometry =
        THREE.PlaneGeometry.Create(container.Dim.Length |> float, container.Dim.Width |> float)

    let planeMaterial =
        THREE.MeshLambertMaterial.Create(jsOptions<_> (fun x -> x.color <- Some !^ "red"))

    let plane =
        THREE.Mesh.Create(planeGeometry, planeMaterial)

    plane.receiveShadow <- true
    plane.rotation.x <- -0.5 * Math.PI
    plane.position.set (0., 0., 0.) |> ignore
    scene.add plane |> ignore
    currentContainer.Add plane


let cubeMaterial =
    THREE.MeshLambertMaterial.Create
        (jsOptions<Three.MeshLambertMaterialParameters> (fun x ->
            x.color <- Some !^ "green"
            x.wireframe <- Some true))

let wireframeMaterial =
    THREE.MeshBasicMaterial.Create
        (jsOptions<Three.MeshBasicMaterialParameters> (fun x ->
            x.wireframe <- Some true
            x.transparent <- Some true
            x.color <- Some !^ "black"))

let lineMaterial =
    THREE.LineBasicMaterial.Create(jsOptions<Three.LineBasicMaterialParameters> (fun x -> x.color <- Some !^ "black"))

let cubes = ResizeArray<Three.Object3D>()

let renderCube x y z width height length (color: string) L W =
    let cubeMaterial =
        THREE.MeshLambertMaterial.Create
            (jsOptions<Three.MeshLambertMaterialParameters> (fun x ->
                x.color <- Some !^color
                x.wireframe <- Some false))

    let cubeGeometry =
        THREE.BoxGeometry.Create(length, height, width)

    let edgeGeomerty =
        THREE.EdgesGeometry.Create !^cubeGeometry

    let wireFrame =
        THREE.LineSegments.Create(edgeGeomerty, lineMaterial)

    let cube =
        THREE.Mesh.Create(cubeGeometry, cubeMaterial)

    cube.add wireFrame |> ignore

    cube.castShadow <- true
    // let container = currentContainer.[0] :?> Three.Mesh<Three.PlaneGeometry,_>
    // let v = THREE.Vector3.Create()
    // let cont = THREE.Box3.Create().setFromObject(container).getSize(v)
    // let L = cont.x
    // let W = cont.z
    cube.position.set (z - (L - length) / 2., y + height / 2., (W - width) / 2. - x)
    |> ignore
    scene.add cube |> ignore
    cubes.Add cube


let init () =
    let addSpottLight x y z inten =
        let spotLight = THREE.SpotLight.Create(!^ "white")
        spotLight.position.set (x, y, z) |> ignore
        spotLight.castShadow <- true
        spotLight.shadow.mapSize <- THREE.Vector2.Create(1024., 1024.)
        spotLight.shadow.camera?far <- 130
        spotLight.shadow.camera?near <- 40
        spotLight.intensity <- inten
        scene.add (spotLight) |> ignore

    addSpottLight -400. 400. 150. 0.4
    addSpottLight 400. 400. 450. 0.4
    addSpottLight -800. 1200. 2450. 0.4
    addSpottLight -1400. 1000. 1450. 0.4

    let dLight =
        THREE.DirectionalLight.Create(!^ "white", 0.4)

    dLight.translateX 100.0 |> ignore
    dLight.rotateZ 40. |> ignore
    dLight.rotateX 10. |> ignore
    dLight.rotateY 10. |> ignore
    dLight.position.set (-400., 400., -900.) |> ignore
    scene.add dLight |> ignore

    let dLight2 =
        THREE.DirectionalLight.Create(!^ "white", 0.4)

    dLight2.translateX 100.0 |> ignore
    dLight2.rotateZ 40. |> ignore
    dLight2.rotateX 10. |> ignore
    dLight2.rotateY 10. |> ignore
    dLight2.position.set (400., 400., -900.) |> ignore
    scene.add dLight2 |> ignore

    camera.position.set (500., 550., -700.) |> ignore
    camera.lookAt (!^scene.position)


    //let track = TrackballControls.exports
    let initTrackballControls (camera, (renderer: Three.Renderer)) =
        let trackballControls =
            TrackballControls.TrackballControls.Create(camera, renderer.domElement)

        trackballControls.rotateSpeed <- 1.0
        trackballControls.zoomSpeed <- 1.2
        trackballControls.panSpeed <- 0.8
        trackballControls.noZoom <- false
        trackballControls.noPan <- false
        trackballControls.staticMoving <- true
        trackballControls.dynamicDampingFactor <- 0.3
        trackballControls.keys <- ResizeArray<float>([ 65.; 83.; 68. ])
        trackballControls

    let trackballControls = initTrackballControls (camera, renderer)
    let clock = THREE.Clock.Create()

    let resizeRendererToDisplaySize (renderer: Three.Renderer) =
        let canvas = renderer.domElement
        let width = canvas.clientWidth
        let height = canvas.clientHeight

        let needResize =
            canvas.width <> width || canvas.height <> height

        if (needResize) then renderer.setSize (width, height, false)
        needResize

    let rec renderScene time =
        let time = time * 0.001
        trackballControls.update (clock.getDelta ())
        if (resizeRendererToDisplaySize (renderer)) then
            let canvas = renderer.domElement
            camera.aspect <- canvas.clientWidth / canvas.clientHeight
            camera.updateProjectionMatrix ()
        if demoMode2 then
            cubes
            |> Seq.indexed
            |> Seq.iter (fun (ndx, ob) ->
                let speed = 0.1 + float (ndx) * 0.05
                let rot = time * speed
                ob?rotation?x <- rot
                ob?rotation?y <- rot)
        renderer.render (scene, camera)
        window.requestAnimationFrame (renderScene)
        |> ignore

    renderScene 0.
let renderResultInner container items demoMode =
        demoMode2 <- demoMode
        renderPlane container
        scene.remove cubes |> ignore
        cubes.Clear()
        for (item: ItemPut) in items do
            renderCube
                (item.Coord.X |> float)
                (item.Coord.Y |> float)
                (item.Coord.Z |> float)
                (item.Item.Dim.Width |> float)
                (item.Item.Dim.Height |> float)
                (item.Item.Dim.Length |> float)
                item.Item.Tag
                (container.Dim.Length |> float)
                (container.Dim.Width |> float)

let renderResult (container : Container) items demoMode =
    match lastLController with
    | Some c -> gui.remove(c)
    | _ -> ()
    lastLController <-
        Some (gui.add({|h_filter = 0|}, "h_filter", 0., float(container.Dim.Length))
            .onChange(fun v ->
                renderResultInner container
                    (items |> List.filter
                        (fun i -> float(container.Dim.Length - i.Coord.Z) >= float(!!v))) demoMode2 ) )

    match lastHController with
    | Some c -> gui.remove(c)
    | _ -> ()
    lastHController <-
        Some (gui.add({|v_filter = 0|}, "v_filter", 0., float(container.Dim.Height))
            .onChange(fun v ->
                renderResultInner container
                    (items |> List.filter
                        (fun i -> float(container.Dim.Height - i.Coord.Y) >= float(!!v))) demoMode2 ) )

    gui.updateDisplay()

    renderResultInner container items demoMode
