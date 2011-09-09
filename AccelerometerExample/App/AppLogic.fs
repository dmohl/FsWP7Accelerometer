namespace WindowsPhoneApp

open System
open System.Net
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Ink
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Animation
open System.Windows.Shapes
open System.Windows.Navigation
open Microsoft.Phone.Controls
open Microsoft.Phone.Shell
open AccelerometerProxy

#nowarn "40"
[<AutoOpen>]
module private Utilities = 
    /// This is an implementation of the dynamic lookup operator for binding
    /// Xaml objects by name.
    let (?) (source:obj) (s:string) =
        match source with 
        | :? ResourceDictionary as r ->  r.[s] :?> 'T
        | :? Control as source -> 
            match source.FindName(s) with 
            | null -> invalidOp (sprintf "dynamic lookup of Xaml component %s failed" s)
            | :? 'T as x -> x
            | _ -> invalidOp (sprintf "dynamic lookup of Xaml component %s failed because the component found was of type %A instead of type %A"  s (s.GetType()) typeof<'T>)
        | _ -> invalidOp (sprintf "dynamic lookup of Xaml component %s failed because the source object was of type %A. It must be a control or a resource dictionary" s (source.GetType()))

    let accelerometer = AccelerometerProxy.GetAccelerometer()

/// This type implements the main page of the application
type MainPage() as this =
    inherit PhoneApplicationPage()

    // Load the Xaml for the page.
    do Application.LoadComponent(this, new System.Uri("/WindowsPhoneApp;component/MainPage.xaml", System.UriKind.Relative))
    let contentTextBlock : TextBlock = this?txtContent

    let ProcessAccelerometerData (x:double) (y:double) (z:double) (timestamp:DateTimeOffset) =
        let content = sprintf "X = %f\n Y = %f\n Z=%f\n Magnitude = %f\n\n %O" x y z (sqrt(x * x + y * y + z * z)) timestamp
        this.Dispatcher.BeginInvoke(fun _ -> contentTextBlock.Text <- content) |> ignore

    do accelerometer.ReadingChanged
       |> Event.add ( fun (args:AccelerometerEventArgs) -> 
                          ProcessAccelerometerData args.X args.Y args.Z args.Timestamp )

/// One instance of this type is created in the application host project.
type App(app:Application) = 
    // Global handler for uncaught exceptions. 
    // Note that exceptions thrown by ApplicationBarItem.Click will not get caught here.
    do app.UnhandledException.Add(fun e -> 
            if (System.Diagnostics.Debugger.IsAttached) then
                // An unhandled exception has occurred, break into the debugger
                System.Diagnostics.Debugger.Break();
     )

    let rootFrame = new PhoneApplicationFrame();

    do app.RootVisual <- rootFrame;

    // Handle navigation failures
    do rootFrame.NavigationFailed.Add(fun _ -> 
        if (System.Diagnostics.Debugger.IsAttached) then
            // A navigation has failed; break into the debugger
            System.Diagnostics.Debugger.Break())

    // Navigate to the main page 
    do rootFrame.Navigate(new Uri("/WindowsPhoneApp;component/MainPage.xaml", UriKind.Relative)) |> ignore

    // Required object that handles lifetime events for the application
    let service = PhoneApplicationService()
    // Code to execute when the application is launching (eg, from Start)
    // This code will not execute when the application is reactivated
    do service.Launching.Add(fun _ -> accelerometer.Start())
    // Code to execute when the application is closing (eg, user hit Back)
    // This code will not execute when the application is deactivated
    do service.Closing.Add(fun _ -> accelerometer.Stop())
    // Code to execute when the application is activated (brought to foreground)
    // This code will not execute when the application is first launched
    do service.Activated.Add(fun _ -> accelerometer.Start())
    // Code to execute when the application is deactivated (sent to background)
    // This code will not execute when the application is closing
    do service.Deactivated.Add(fun _ -> accelerometer.Stop())

    do app.ApplicationLifetimeObjects.Add(service) |> ignore
