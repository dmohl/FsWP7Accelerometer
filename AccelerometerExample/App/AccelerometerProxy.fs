module AccelerometerProxy

open System
open System.Windows
open Microsoft.Devices
open Microsoft.Devices.Sensors

type AccelerometerEventArgs() =
    inherit EventArgs()
    let mutable x = 0.0
    let mutable y = 0.0
    let mutable z = 0.0
    let mutable timestamp = DateTimeOffset(DateTime.Now)
    member this.X with get() = x and set v = x <- v
    member this.Y with get() = y and set v = y <- v
    member this.Z with get() = z and set v = z <- v
    member this.Timestamp with get() = timestamp and set v = timestamp <- v

type DeviceAccelerometer(startFunc, stopFunc) = 
    let readingChangedEvent = Event<EventHandler<AccelerometerEventArgs>, AccelerometerEventArgs>()
    member x.Start () = readingChangedEvent |> startFunc x
    member x.Stop () = stopFunc()
    member x.ReadingChanged = readingChangedEvent.Publish
    member x.OnReadingChanged eventArgs = readingChangedEvent.Trigger(x, eventArgs)

let DefaultAccelerometer() = 
    let accelerometer = new Accelerometer()
    
    let startFunc x event = 
        (fun (x, event:Event<EventHandler<AccelerometerEventArgs>, AccelerometerEventArgs>) -> 
            accelerometer.ReadingChanged.Add(fun e -> event.Trigger(x, 
                                                          AccelerometerEventArgs(X=e.X, Y=e.Y, Z=e.Z, 
                                                              Timestamp=e.Timestamp)))
            do accelerometer.Start()
        )(x, event)

    let stopFunc () = (fun() -> accelerometer.Stop())()
    (startFunc, stopFunc)

let MouseAccelerometer() = 
    let mouseDownLocation = ref (new Point())
    let isMouseDown = ref false
    let tolerance = 0.001
    let target = Application.Current.RootVisual :?> FrameworkElement
    
    let startFunc x event = 
        (fun (x, event:Event<EventHandler<AccelerometerEventArgs>, AccelerometerEventArgs>) -> 
            target.MouseLeftButtonDown.Add(fun e -> if (target.CaptureMouse()) then
                                                        isMouseDown := true
                                                        mouseDownLocation := e.GetPosition(target))

            target.MouseMove.Add(fun e -> if !isMouseDown then
                                              let position = e.GetPosition target
                                              let x = (position.X - (!mouseDownLocation).X) * tolerance
                                              let y = -(position.Y - (!mouseDownLocation).Y) * tolerance
                                              let timestamp = new DateTimeOffset(DateTime.Now)
                                              event.Trigger(x, 
                                                  new AccelerometerEventArgs(X=x, Y=y, Z=0.0, Timestamp=timestamp)) )

            target.MouseLeftButtonUp.Add(fun e -> target.ReleaseMouseCapture())

            target.LostMouseCapture.Add(fun e -> isMouseDown := false)
        )(x, event)

    let stopFunc () = if (!isMouseDown) then
                          target.ReleaseMouseCapture()
                          isMouseDown := false
    (startFunc, stopFunc)

let GetAccelerometer() =
    let (startFunc, stopFunc) = match Microsoft.Devices.Environment.DeviceType with
                                | DeviceType.Device -> DefaultAccelerometer()
                                | _ -> MouseAccelerometer()
    DeviceAccelerometer(startFunc, stopFunc)
