module CanvasRenderer

open Fable.Core
open Browser.Dom
open Fable.Core.JsInterop
open Fable.Core.JS
open System
open Shared

let THREE = Three.exports
let scene = THREE.Scene.Create()

let camera =
    THREE.PerspectiveCamera.Create(45., window.innerWidth / window.innerHeight, 0.1, 2000.)

let opt =
    jsOptions<Three.WebGLRendererParameters>
        (fun x-> x.antialias <- Some true ; x.canvas <- Some (!^(document.getElementById("myCanvas"))))
let renderer =
    THREE.WebGLRenderer.Create(opt)

renderer.setClearColor (!^THREE.Color.Create(!^ (float 0xFFFFFF)))
renderer.setSize (window.innerWidth, window.innerHeight)
renderer.shadowMap?enabled <- true

let axes = THREE.AxesHelper.Create(20.)
scene.add (axes) |> ignore
//let L = 1300.
//let W = 245.
let currentContainer = ResizeArray<Three.Object3D>()
let renderPlane (container:Container)  =
    scene.remove currentContainer |> ignore
    let planeGeometry =
        THREE.PlaneGeometry
            .Create(container.Dim.Length |> float, container.Dim.Width|>float)

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
    THREE.MeshLambertMaterial.Create(jsOptions<Three.MeshLambertMaterialParameters>
        (fun x -> x.color <- Some !^ "green"; x.wireframe<- Some true ))
let wireframeMaterial =
    THREE.MeshBasicMaterial.Create(jsOptions<Three.MeshBasicMaterialParameters>
        (fun x-> x.wireframe <- Some true; x.transparent <- Some true; x.color <- Some !^ "black") )

let lineMaterial =
    THREE.LineBasicMaterial.Create(jsOptions<Three.LineBasicMaterialParameters>
        (fun x->  x.color <- Some !^ "black") )
let cubes = ResizeArray<Three.Object3D>()
let renderCube x y z width height length (color:string) L W =
    let cubeMaterial =
        THREE.MeshLambertMaterial.Create(jsOptions<Three.MeshLambertMaterialParameters>
            (fun x -> x.color <- Some !^ color ; x.wireframe<- Some false   ))

    let cubeGeometry = THREE.BoxGeometry.Create(length, height, width )
    let edgeGeomerty = THREE.EdgesGeometry.Create !^cubeGeometry
    let wireFrame = THREE.LineSegments.Create(edgeGeomerty , lineMaterial)
    let cube =
        THREE.Mesh.Create(cubeGeometry, cubeMaterial)

    cube.add wireFrame |> ignore

    cube.castShadow <- true
    // let container = currentContainer.[0] :?> Three.Mesh<Three.PlaneGeometry,_>
    // let v = THREE.Vector3.Create()
    // let cont = THREE.Box3.Create().setFromObject(container).getSize(v)
    // let L = cont.x
    // let W = cont.z
    cube.position.set ( z - (L - length) /2. , y + height / 2. , (W - width) / 2. - x) |> ignore
    scene.add cube |> ignore
    cubes.Add cube


let containers = {Dim = {Width =int 245; Height = 150; Length = int 1300}; Coord = {X =0; Y =0; Z =0 }}
let items = [
    {Dim = {Width = 100; Height = 100; Length = 14} ;Id = "big1" ; Tag ="#880000"};
      {Dim = {Width = 100; Height = 46; Length = 100} ; Id = "big2";Tag ="blue"};
      {Dim = {Width = 45; Height = 37; Length = 30} ; Id = "small1";Tag ="pink"};
      {Dim = {Width = 45; Height = 19; Length = 30} ; Id = "small22";Tag ="lime"};
         {Dim = {Width = 100; Height = 56; Length = 100} ;Id = "big3" ; Tag ="green"};
      {Dim = {Width = 100; Height = 88; Length = 39} ; Id = "big4";Tag ="blue"};
     {Dim = {Width = 45; Height = 27; Length = 30} ; Id = "2";Tag ="pink"};
     {Dim = {Width = 45; Height = 89; Length = 30} ; Id = "3";Tag ="lime"};
    //       {Dim = {Width = 100; Height = 100; Length = 100} ;Id = "1" ; Tag ="green"};
    // {Dim = {Width = 100; Height = 95; Length = 47} ; Id = "4";Tag ="blue"};
    //  {Dim = {Width = 45; Height = 100; Length = 30} ; Id = "5";Tag ="pink"};
    //  {Dim = {Width = 45; Height = 83; Length = 30} ; Id = "6";Tag ="lime"};
    //  {Dim = {Width = 100; Height = 100; Length = 25} ;Id = "7" ; Tag ="green"};
    //  {Dim = {Width = 100; Height = 13; Length = 10} ; Id = "8";Tag ="blue"};
    //  {Dim = {Width = 150; Height = 125; Length = 10} ; Id = "9"; Tag ="red"};
    //  {Dim = {Width = 50; Height = 111; Length = 56} ; Id = "10";Tag ="yellow"};
    //  {Dim = {Width = 5; Height = 75; Length = 35} ; Id = "11";Tag ="aqua"};
    //  {Dim = {Width = 50; Height = 150; Length = 56} ; Id = "12";Tag ="pink"};
    //  {Dim = {Width = 50; Height = 75; Length = 124} ; Id = "13";Tag ="white"};
    //  {Dim = {Width = 15; Height = 150; Length = 30} ; Id = "14";Tag ="navy"};
    //  {Dim = {Width = 50; Height = 100; Length = 30} ; Id = "15";Tag ="aqua"};
    //       {Dim = {Width = 50; Height = 150; Length = 200} ; Id = "16";Tag ="yellow"};
    //  {Dim = {Width = 50; Height = 148; Length = 25} ; Id = "17";Tag ="aqua"};
    //  {Dim = {Width = 25; Height = 75; Length = 99} ; Id = "18";Tag ="pink"};
    //  {Dim = {Width = 85; Height = 127; Length = 111} ; Id = "19";Tag ="green"};
    //  {Dim = {Width = 95; Height = 75; Length = 30} ; Id = "20";Tag ="navy"};
    //  {Dim = {Width = 50; Height = 27; Length = 30} ; Id = "21";Tag ="aqua"};
    //       {Dim = {Width = 50; Height = 150; Length = 200} ; Id = "22";Tag ="pink"};
    //  {Dim = {Width = 150; Height = 131; Length = 200} ; Id = "23";Tag ="white"};
    //  {Dim = {Width = 40; Height = 15; Length = 30} ; Id = "24";Tag ="navy"};
    //  {Dim = {Width = 47; Height = 100; Length = 30} ; Id = "25";Tag ="aqua"};
    //          {Dim = {Width = 50; Height = 150; Length = 200} ; Id = "26";Tag ="pink"};
    //  {Dim = {Width = 59; Height = 125; Length = 200} ; Id = "27";Tag ="white"};
    //  {Dim = {Width = 67; Height = 75; Length = 30} ; Id = "28";Tag ="navy"};
    //  {Dim = {Width = 89; Height = 100; Length = 30} ; Id = "29";Tag ="aqu"};
    //    {Dim = {Width = 61; Height = 47; Length = 99} ; Id = "30";Tag ="#222222"};
    //  {Dim = {Width = 143; Height = 145; Length = 135} ; Id = "31";Tag ="white"};
    //  {Dim = {Width = 131; Height = 75; Length = 30} ; Id = "32";Tag ="navy"};
    //   {Dim = {Width = 21; Height = 100; Length = 30} ; Id = "33";Tag ="aqua"};
    //       {Dim = {Width = 50; Height = 150; Length = 200} ; Id = "34";Tag ="yellow"};
    //  {Dim = {Width = 40; Height = 42; Length = 84} ; Id = "35";Tag ="aqua"};
    //  {Dim = {Width = 99; Height = 47; Length = 21} ; Id = "36";Tag ="pink"};
    //     {Dim = {Width = 50; Height = 125; Length = 200} ; Id = "27";Tag ="white"};
    //  {Dim = {Width = 50; Height = 75; Length = 30} ; Id = "28";Tag ="navy"};
    //  {Dim = {Width = 50; Height = 100; Length = 30} ; Id = "29";Tag ="aqu"};
    //    {Dim = {Width = 50; Height = 150; Length = 200} ; Id = "30";Tag ="#222222"};
    //  {Dim = {Width = 97; Height = 150; Length = 200} ; Id = "31";Tag ="white"};
    //  {Dim = {Width = 42; Height = 75; Length = 30} ; Id = "32";Tag ="navy"};
    //   {Dim = {Width = 36; Height = 100; Length = 30} ; Id = "33";Tag ="aqua"};
    //       {Dim = {Width = 50; Height = 150; Length = 200} ; Id = "34";Tag ="yellow"};
    //  {Dim = {Width = 39; Height = 150; Length = 24} ; Id = "35";Tag ="aqua"};
    //  {Dim = {Width = 39; Height = 16; Length = 200} ; Id = "36";Tag ="pink"};
    ]

let renderResult res =
    renderPlane res.Container
    scene.remove cubes |> ignore
    cubes.Clear()
    for item in res.ItemsPut do
        renderCube (item.Coord.X |> float) (item.Coord.Y |> float)
            (item.Coord.Z |> float) (item.Item.Dim.Width |> float)
            (item.Item.Dim.Height |> float) (item.Item.Dim.Length |> float) item.Item.Tag
            (res.Container.Dim.Length |> float)
            (res.Container.Dim.Width |> float)

    let addSpottLight x y z =
        let spotLight = THREE.SpotLight.Create(!^ "white")
        spotLight.position.set (x, y, z) |> ignore
        spotLight.castShadow <- true
        spotLight.shadow.mapSize <- THREE.Vector2.Create(1024., 1024.)
        spotLight.shadow.camera?far <- 130
        spotLight.shadow.camera?near <- 40
        scene.add (spotLight) |> ignore

    addSpottLight -400. 400. 150.
    addSpottLight 400. 400. 450.
    addSpottLight -400. 400. 1450.
    addSpottLight -1400. 1000. 1450.

    let dLight = THREE.DirectionalLight.Create(!^ "white",0.5)
    dLight.translateX 100.0 |>ignore
    dLight.rotateZ 40. |> ignore
    dLight.rotateX 10. |> ignore
    dLight.rotateY 10. |> ignore
    dLight.position.set (-400., 400., -900.) |> ignore
    scene.add dLight |> ignore
    let dLight2 = THREE.DirectionalLight.Create(!^ "white",0.5)
    dLight2.translateX 100.0 |>ignore
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

    let mutable step = 0.

    let rec renderScene _ =
        trackballControls.update (clock.getDelta ())
        window.requestAnimationFrame (renderScene)
        |> ignore
        renderer.render (scene, camera)

    renderScene 0.

