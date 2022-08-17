# Inter-process Communication (One-Way)

This example uses named pipes & memory streams for local network inter-process communication in C#. The IPC however is one-way client-to-server. A client connects, posts it's info to the server, then disconnects. The server then prompts an event for the received information.

The InterProcComm class acts as a backend for IPC by listening for incoming client connections on the named pipe server using the application's GUID. Named pipes should always have a unique name to avoid connection collision between different applications--thus GUID. This works as long as your IPC processes are working across the same application/assembly.

We listen asynchronously on the named pipe for a synchronous client connection. Once the connection is established, immediately read incoming data and then close the connection. We can read/write to the named pipe on the client via the provided MemoryBuffer which can be passed to the named pipe's backend stream.

```c#
// Start the IPC immediately and subscribe to MessageReceived.
InterProcComm procComm = new InterProcComm(true);
procComm.MessageReceived += ProcessMessage;
```
```c#
// Posted messages to InterProcComm.
MemoryBuffer buffer = new MemoryBuffer(0);
buffer.Write("This is a string.");
InterProcComm.Post(buffer);
```
```c#
public void ProcessMessage(MemoryBuffer buffer) {
  string message = "";
  buffer.Read(message);
  Console.WriteLine(message);
}
```

--
#### Single Instance WPF Application via IPC
*See [The Misunderstood Mutex](http://odetocode.com/blogs/scott/archive/2004/08/20/the-misunderstood-mutex.aspx) for the original source and information.*

*Creating a single instance application while catching all of the edge cases can be trickey. Luckily, with the above mutex solution and the One-Way InterProcComm class, we can make this, really easy.*

Create a new WPF application or open an existing application. Right-click your App.xaml, click Properties, then in the properties panel change Build Action from ApplicationDefinition to Page. This tells WPF that we want our App to be treated as a normal resource instead of as an application. This removes the implicit Main() method call and forces us to create one manually.

To do this, we can open up our App.xaml.cs file and directly inject a new `Main()` method into our App class. We'll also need to create the App class a new constructor so we can call `InitializeComponent()` on it. *See also [`InitializeComponent`](http://stackoverflow.com/questions/245825/what-does-initializecomponent-do-and-how-does-it-work-in-wpf) and [`STAThread`](http://stackoverflow.com/questions/1361033/what-does-stathread-do).*
```c#
using System;
using System.Threading;
using System.Windows;
using System.Reflection;

public partial class App : Application {
    App() {
        // Call InitializeComponent() to display our application.
        InitializeComponent();
    }
    
    [STAThread]
    public static void Main(params string[] args) {
        // Create an instance of our App class to urn the application.
        App application = new App();
        application.Run();
    }
}
```
So no we have a new `Main()` method and our application will open again. Now we need to implement the mutex pattern from *[The Misunderstood Mutex](http://odetocode.com/blogs/scott/archive/2004/08/20/the-misunderstood-mutex.aspx)* into our `Main()` method.
```c#
[STAThread]
public static void Main(params string[] args) {
    // Get the executing assembly's GUID for use as a unique identifier.
    string guid = Assembly.GetEntryAssembly().GetCustomAttributes<GuidAttribute>().ToString();
    
    using(Mutex mx = new Mutex(false, guid)) {
        // If another process has hold of the named mutex, then return/close.
        if (!mx.WaitOne(0, false))
            return;
        
        // If we got control of the named mutex, run our application and clean up the mutex.
        GC.Collect();
        App application = new App();
        application.Run();
    }
}
```
Finally we have the mutex pattern implemented and we have our single instance application. This means the first instance of the application to open will be the only instance to stay open. Successive instances will be killed. Now we need to impliment IPC via the InterProcComm class in order to get successive instances to pass their command-line parameters to the first instance of the applciation.

We'll do this by instantiating InterProcComm in the App class' constructor and storing it in a property. Next we'll need to use the static method `InterProcComm.Post(MemoryBuffer)` in order to send our command-line parameters to the existing first instance process of our application if the running process is NOT the first instance. We'll do this by creating a new `MemoryBuffer` and writing each command-line parmeter to it as a string via `Write(string)`.
```c#
public partial class App : Application {
    // Holds the instance to the running InterProcComm;
    public InterProcComm IPC { get; private set; } = null;
    
    App() {
        // Create InterProcComm instance.
        IPC = new InterProcComm(true);
        InitializeComponent();
    }
    
    [STAThread]
    public static void Main(params string[] args) {
        string guid = Assembly.GetEntryAssembly().GetCustomAttributes<GuidAttribute>().ToString();
        
        using(Mutex mx = new Mutex(false, guid)) {
            // If another process has hold of the named mutex, send our Post(), then return/close.
            if (!mx.WaitOne(0, false)) {
                MemoryBuffer mb = new MemoryBuffer();
                
                foreach(string str in args)
                  mb.Write(str);
                  
                InterProcComm.Post(mb);
                return;
            }
            
            GC.Collect();
            App application = new App();
            application.Run();
        }
    }
}
```
Finally we need to subscribe to the `MessageReceived(MemoryBuffer)` event in our App class' instance of InterProcComm. Then handle our event args from there...
```c#
public partial class App : Application {
    public InterProcComm IPC { get; private set; } = null;
    
    App() {
        IPC = new InterProcComm(true);
        
        // Subscribe to MessageReceived event.
        IPC.MessageReceived += ReceivedMessage;
        InitializeComponent();
    }
    
    private void ReceivedMessage(MemoryBuffer buffer) {
        // Read MemoryBuffer to process received command-line parameters...
        while(buffer.Position < buffer.Length) {
          string param = string.Empty;
          buffer.Read(out param);
          Console.WriteLine(param);
        }
    }
    
    [STAThread]
    public static void Main(params string[] args) {
        ...
    }
}
```
Final Result:
```c#
using System;
using System.Threading;
using System.Windows;
using System.Reflection;

public partial class App : Application {
    public InterProcComm IPC { get; private set; } = null;
    
    App() {
          IPC = new InterProcComm(true);
          
          // Subscribe to MessageReceived event.
          IPC.MessageReceived += ReceivedMessage;
          InitializeComponent();
    }
    
    private void ReceivedMessage(MemoryBuffer buffer) {
        // Read MemoryBuffer to process received command-line parameters...
        while(buffer.Position < buffer.Length) {
          string param = string.Empty;
          buffer.Read(out param);
          Console.WriteLine(param);
        }
    }
    
    [STAThread]
    public static void Main(params string[] args) {
        string guid = Assembly.GetEntryAssembly().GetCustomAttributes<GuidAttribute>().ToString();
        
        using(Mutex mx = new Mutex(false, guid)) {
            // If another process has hold of the named mutex, send our Post(), then return/close.
            if (!mx.WaitOne(0, false)) {
                MemoryBuffer mb = new MemoryBuffer();
                
                foreach(string str in args)
                  mb.Write(str);
                  
                InterProcComm.Post(mb);
                return;
            }
            
            // If we got control of the named mutex, run our application and clean up the mutex.
            GC.Collect();
            App application = new App();
            application.Run();
        }
    }
}
```
