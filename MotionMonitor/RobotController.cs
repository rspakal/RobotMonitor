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
    public class RobotController
    {
        private readonly IPAddress _ipAddress;
        public readonly Socket socket;
        private const int PORT = 4011;
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const int CONNECTION_TIMEOUT = 500;
        public ConnectionState connectionState;
        private ManualResetEvent _connectionComplete;
        private ManualResetEvent _commandExecuted;
        public event EventHandler<PropertyChangedEventArgs<ConnectionState>> ConnectionStateChanged;
        public event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
        public RobotController()
        {
            _ipAddress = new(new byte[4] { 192, 168, 125, 1 });
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
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
