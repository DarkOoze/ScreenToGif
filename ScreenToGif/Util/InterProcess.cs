using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System.Windows;

namespace ScreenToGif.Util
{
    public static partial class InterProcess
    {
        private const string PipeName = "ScreenToGit.Process";

        private static PipeServer<IpcMessage> _server;

        internal static void RegisterServer()
        {
            try
            {
                if (_server != null)
                    return;

                using (var process = Process.GetCurrentProcess())
                {
                    _server = new PipeServer<IpcMessage>(PipeName + process.Id);
                }

                _server.MessageReceived += ServerOnMessageReceived;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to register the IPC server.");
            }
        }

        internal static void UnregisterServer()
        {
            try
            {
                _server.Stop();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to unregister the IPC server.");   
            }
        }

        internal static void SendMessage(int processId, string[] args)
        {
            try
            {
                using (var pipe = new NamedPipeClientStream(".", PipeName + processId, PipeDirection.Out))
                {
                    pipe.Connect();

                    var message = new IpcMessage { Args = args };
                    var buffer = JsonSerializer.SerializeToUtf8Bytes(message);

                    pipe.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to send a message via the IPC server.");
            }
        }

        private static void ServerOnMessageReceived(object sender, IpcMessage message)
        {
            try
            {
                var args = message.Args;

                if (args?.Length > 0)
                    Argument.Prepare(args);

                App.MainViewModel.OpenEditor.Execute(args);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Unable to execute arguments from IPC.");
            }
        }

        private struct IpcMessage
        {
            public string[] Args { get; set; }
        }
    }
}