using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace InterProcessCommunication {
    public class InterProcComm : IDisposable {
        private static string appGUID = Assembly.GetExecutingAssembly().GetType().GUID.ToString();
        private CancellationTokenSource cancellationSource;
        private Task procCommTask;
        private bool canListen;

        public delegate void MessageReceivedEventHandler(GrowBuffer message);
        public event MessageReceivedEventHandler MessageReceived;

        public InterProcComm(bool start) {
            if (start)
                Start();
        }

        private void Listen() {
            while(!cancellationSource.IsCancellationRequested) {
                using(NamedPipeServerStream pipeServer = new NamedPipeServerStream(appGUID)) {
                    try {
                        pipeServer.WaitForConnection();
                        GrowBuffer memory = new GrowBuffer(0);
                        memory.Grow(pipeServer.InBufferSize);
                        pipeServer.Read(memory.GetBuffer(), 0, pipeServer.InBufferSize);
                        MessageReceived?.Invoke(memory);
                        pipeServer.Disconnect();
                    } catch(IOException) {
                        pipeServer.Disconnect();
                    }
                }
            }

            procCommTask.Dispose();
            cancellationSource.Dispose();
            procCommTask = null;
            cancellationSource = null;
        }

        public static bool Post(GrowBuffer memory) {
            using(NamedPipeClientStream pipeClient = new NamedPipeClientStream(appGUID)) {
                pipeClient.Connect(50);

                if(pipeClient.IsConnected) {
                    pipeClient.Write(memory.GetBuffer(), 0, memory.Iterator);
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
            cancellationSource.Dispose();
            procCommTask.Dispose();

            if(disposeManaged) {
                cancellationSource = null;
                procCommTask = null;
            }
        }
    }
}
