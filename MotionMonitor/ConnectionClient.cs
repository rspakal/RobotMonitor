using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class ConnectionClient
    {
        private readonly byte[] _ipAdressBytes = { 192, 168, 125, 1 };
        private readonly IPAddress _ipAddress;
        private readonly Socket _socket;
        private const int PORT = 4011;
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const int CONNECTION_TIMEOUT = 500;
        public ConnectionState connectionState;
        private ManualResetEvent _connectionComplete;
        private ManualResetEvent _commandExecuted;
        public event EventHandler<PropertyChangedEventArgs<ConnectionState>> ConnectionStateChanged;
        public event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
        public ConnectionClient()
        {
            _ipAddress = new(_ipAdressBytes);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveBufferSize = RECEIVE_BUFFER_SIZE
            };
            connectionState = ConnectionState.Idle;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource сancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            сancellationTokenSource.CancelAfter(CONNECTION_TIMEOUT);

            try
            {
                SetConnectionState(ConnectionState.Connecting);
                await _socket.ConnectAsync(new IPEndPoint(_ipAddress, PORT), сancellationTokenSource.Token);
                SetConnectionState(ConnectionState.Connected); 
            }
            catch (OperationCanceledException)
            {
                SetConnectionState(ConnectionState.Idle);
            }
            catch (SocketException)
            {
                SetConnectionState(ConnectionState.Idle);
            }
        }

        public async Task DisconnectAsync()
        {
            //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", "Disconnecting!");
            if (connectionState != ConnectionState.Connected)
            {
                return;
            }

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                await _socket.DisconnectAsync(false);
            }
            catch (Exception ex)
            {
                //NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex);
            }

            try
            {
                StopStreamHandler();
                SetConnectionState(ConnectionState.Disconnecting);
                _socket.Close();
            }
            catch (Exception ex2)
            {
                //NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex2);
            }

            try
            {
                SetConnectionState(ConnectionState.Idle);
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", "Disconnect complete!");
            }
            catch (Exception ex3)
            {
                //NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex3);
            }
        }


        private void SetConnectionState(ConnectionState newState)
        {
            var previousState = connectionState;
            bool wasConnected = previousState == ConnectionState.Connected;
            connectionState = newState;
            if (previousState != newState && ConnectionStateChanged != null)
            {
                ConnectionStateChanged(this, new PropertyChangedEventArgs<ConnectionState>(previousState, newState));
            }

            bool isConnected = newState == ConnectionState.Connected;
            if (isConnected != wasConnected && ConnectedChanged != null)
            {
                ConnectedChanged(this, new NotifyEventArgs<bool>(isConnected));
            }
        }
    }
}
