#####Inter-process Communication
This example uses named pipes & grow buffers for local network inter-process communication in C#.

The InterProcComm class acts as a backend for IPC by listening for incoming client connections on the named pipe server using the application's GUID. Named pipes should always have a unique name to avoid connection collision between different applications--thus GUID.

We listen asynchronously on the named pipe for a synchronous client connection. Once the connection is established, immediately read incoming data and then close the connection. We can read/write to the named pipe via the provided GrowBuffer which can be passed to the named pipe's backend stream.

```
// Start the IPC immediately and subscribe to MessageReceived.
InterProcComm procComm = new InterProcComm(true);
procComm.MessageReceived += ProcessMessage;
```
```
// Posted messages to InterProcComm.
GrowBuffer buffer = new GrowBuffer(0);
buffer.Write("This is a string.");
InterProcComm.Post(buffer);
```
```
private void ProcessMessage(GrowBuffer buffer) {
  string message = "";
  buffer.Read(message);
  Console.WriteLine(message);
}
```
