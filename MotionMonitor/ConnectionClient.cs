using MotionMonitor.Enums;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace MotionMonitor
{
    public class ConnectionClient
    {
        private readonly byte[] _ipAddressBytes = { 192, 168, 125, 1 };
        private const int PORT = 4011;

        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const int CONNECTION_TIMEOUT = 500;
        
        private readonly IPAddress _ipAddress;
        private readonly Channel<ConnectionStates> _connectionStateChannel = Channel.CreateUnbounded<ConnectionStates>();
        private readonly object _connectionStateLock = new();
        
        private Socket? _socket;
        private ConnectionStates _currentConnectionState = ConnectionStates.Idle;

        private readonly ManualResetEvent _connectionComplete;
        private readonly ManualResetEvent _commandExecuted;

        public ConnectionStates CurrentConnectionState
        {
            get 
            {
                lock (_connectionStateLock)
                {
                    return _currentConnectionState;
                }
            }
            private set
            {
                lock (_connectionStateLock)
                {
                    _currentConnectionState = value;
                    _connectionStateChannel.Writer.TryWrite(value);
                }
            }
        }

        public ConnectionClient()
        {
            _commandExecuted = new(false);
            _connectionComplete = new(false);
            _ipAddress = new(_ipAddressBytes);
        }

        public async Task<Socket?> InitAsync(CancellationToken cancellationToken = default)
        {
            PrepareSocket();
            await ConnectAsync(cancellationToken);
            return _currentConnectionState == ConnectionStates.Connected ? _socket : null;
            #region
            //using CancellationTokenSource timeoutCts = new(CONNECTION_TIMEOUT);
            //using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            //timeoutCts.CancelAfter(CONNECTION_TIMEOUT);

            //try
            //{
            //    PrepareSocket();
            //    //SetConnectionState(ConnectionState.Connecting);
            //    CurrentConnectionState = ConnectionStates.Connecting;

            //    await _socket.ConnectAsync(new IPEndPoint(_ipAddress, _port), linkedCts.Token);

            //    if (!_socket.Connected)
            //    {
            //        throw new SocketException((int)SocketError.NotConnected);
            //    }

            //    CurrentConnectionState = ConnectionStates.Connecting;
            //}
            //catch (OperationCanceledException)
            //{
            //    //NotifyMessage("Connection timed out.");
            //    CurrentConnectionState = ConnectionStates.Idle;
            //}
            //catch (SocketException ex)
            //{
            //    //NotifyMessage($"Socket error: {ex.Message}");
            //    CurrentConnectionState = ConnectionStates.Idle;
            //}
            //catch (Exception ex)
            //{
            //    //NotifyMessage($"Unexpected error: {ex.Message}");
            //    CurrentConnectionState = ConnectionStates.Idle;         
            //}
            #endregion
        }

        private void PrepareSocket()
        {
            if (_socket != null)
            {
                try
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch 
                {
                    
                }

                _socket.Dispose();
            }

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveBufferSize = RECEIVE_BUFFER_SIZE };
        }

        private async Task<bool> IsHostAvailableAsync(IPAddress ipAddress, int port, int timeoutMs)
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);
            var timeoutTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            return completedTask == connectTask && client.Connected;
        }

        private async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource timeoutCts = new(CONNECTION_TIMEOUT);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            timeoutCts.CancelAfter(CONNECTION_TIMEOUT);

            try
            {
                CurrentConnectionState = ConnectionStates.Connecting;

                await _socket.ConnectAsync(new IPEndPoint(_ipAddress, PORT), linkedCts.Token);

                if (!_socket.Connected)
                {
                    throw new SocketException((int)SocketError.NotConnected);
                }

                CurrentConnectionState = ConnectionStates.Connected;
                return;
            }
            catch (OperationCanceledException)
            {
                //NotifyMessage("Connection timed out.");
                CurrentConnectionState = ConnectionStates.Idle;
            }
            catch (SocketException ex)
            {
                //NotifyMessage($"Socket error: {ex.Message}");
                CurrentConnectionState = ConnectionStates.Idle;
            }
            catch (Exception ex)
            {
                //NotifyMessage($"Unexpected error: {ex.Message}");
                CurrentConnectionState = ConnectionStates.Idle;
            }
        }

        public async Task DisconnectAsync()
        {
            //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", "Disconnecting!");
            if (_currentConnectionState != ConnectionStates.Connected)
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
                //StopStreamHandler();
                CurrentConnectionState = ConnectionStates.Disconnecting;
                _socket.Close();
            }
            catch (Exception ex2)
            {
                //NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex2);
            }

            try
            {
                CurrentConnectionState = ConnectionStates.Idle;
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", "Disconnect complete!");
            }
            catch (Exception ex3)
            {
                //NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex3);
            }
        }

        private async Task MonitorConnectionStateAsync(Func<ConnectionStates, Task> onConnectionStateChanged, CancellationToken cancellationToken = default)
        {
            await foreach (var state in _connectionStateChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await onConnectionStateChanged(state);
            }
        }

    }
}
