using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;

namespace InterProcessCommunication {
    public class InterProcComm : IDisposable {
        private static string appGUID = Assembly.GetEntryAssembly().GetCustomAttributes<GuidAttribute>().ToString();
        private CancellationTokenSource cancellationSource;
        private Task procCommTask;

        public delegate void MessageReceivedEventHandler(MemoryBuffer message);
        public event MessageReceivedEventHandler MessageReceived;

        public InterProcComm(bool start) {
            if(start)
                Start();
        }

        private void Listen() {
            while(!cancellationSource.IsCancellationRequested) {
                using(NamedPipeServerStream pipeServer = new NamedPipeServerStream(appGUID, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte)) {
                    try {
                        pipeServer.WaitForConnection();

                        MemoryBuffer mb = new MemoryBuffer();
                        int data = 0;
                        while((data = pipeServer.ReadByte()) != -1)
                            mb.Write((byte)data);
                        mb.Memory.Seek(0, SeekOrigin.Begin);
                        MessageReceived?.Invoke(mb);

                        pipeServer.Flush();
                        pipeServer.Disconnect();
                    } catch(Exception) {}
                }
            }

            procCommTask?.Dispose();
            cancellationSource?.Dispose();
            procCommTask = null;
            cancellationSource = null;
        }

        public static bool Post(MemoryBuffer memory) {
            using(NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", appGUID, PipeDirection.Out)) {
                pipeClient.Connect(50);

                if(pipeClient.IsConnected) {
                    byte[] buffer = memory.Memory.GetBuffer();
                    pipeClient.Write(buffer, 0, buffer.Length);
                    pipeClient.Flush();
                }

                bool posted = pipeClient.IsConnected;
                pipeClient.Dispose();
                return posted;
            }
        }

        public void Start() {
            if(cancellationSource == null && procCommTask == null) {
                cancellationSource = new CancellationTokenSource();
                procCommTask = new Task(() => Listen(), cancellationSource.Token, TaskCreationOptions.LongRunning);
                procCommTask.Start();
            }
        }

        ~InterProcComm() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposeManaged) {
            cancellationSource?.Cancel();
            procCommTask?.Wait();

            if(disposeManaged) {
                cancellationSource = null;
                procCommTask = null;
            }
        }
    }
}
