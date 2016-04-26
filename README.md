#####Inter-process Communication (One-Way)
This example uses named pipes & memory streams for local network inter-process communication in C#. The IPC however is one-way client-to-server. A client connects, posts it's info to the server, then disconnects. The server then promnpts an event for the received information.

The InterProcComm class acts as a backend for IPC by listening for incoming client connections on the named pipe server using the application's GUID. Named pipes should always have a unique name to avoid connection collision between different applications--thus GUID. This works as long as your IPC processes are working across the same application/assembly.

We listen asynchronously on the named pipe for a synchronous client connection. Once the connection is established, immediately read incoming data and then close the connection. We can read/write to the named pipe on the client via the provided MemoryBuffer which can be passed to the named pipe's backend stream.

```
// Start the IPC immediately and subscribe to MessageReceived.
InterProcComm procComm = new InterProcComm(true);
procComm.MessageReceived += ProcessMessage;
```
```
// Posted messages to InterProcComm.
MemoryBuffer buffer = new MemoryBuffer(0);
buffer.Write("This is a string.");
InterProcComm.Post(buffer);
```
```
public void ProcessMessage(MemoryBuffer buffer) {
  string message = "";
  buffer.Read(message);
  Console.WriteLine(message);
}
```
