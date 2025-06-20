using System.Net.Sockets;

namespace MotionMonitor
{
    public class DataStreamHandler
    {
        private enum DataMessage
        {
            LogData = 7,
            SignalDefined = 51,
            LoggingStarted = 52,
            LoggingStopped = 53,
            SignalRemoved = 54,
            AllSignalsRemoved = 55,
            SignalsEnumerated = 56,
            Error = 60
        }
        private const int READ_BUFFER_SIZE = 7712;
        private const int ERROR_MESSAGE_SIZE = 80;
        private const int SIGNAL_DEFINITION_MESSAGE_SIZE = 60;
        private const int READ_TIMEOUT = 1000;

        private MotionDataProvider _dataProvider;
        private readonly RobotController _controller;
        private NetworkStream? _networkStream;
        private bool _isReading;
        private Thread? _readThread;
        private int _sleepCounter;

        public DataStreamHandler(MotionDataProvider dataProvider)
        {
            _controller = new RobotController();
            _dataProvider = dataProvider;

        }
        public async Task<bool> StartAsync()
        {
            try
            {
                while (_controller is not null && _controller.connectionState != ConnectionState.Connected)
                {
                    await _controller.ConnectAsync();
                }

                if (_controller.socket is null)
                {
                    throw new NullReferenceException("Socket instance is null");
                }

                _networkStream = new NetworkStream(_controller.socket, false)
                {
                    ReadTimeout = READ_TIMEOUT
                };

                _isReading = true;
                _readThread = new Thread(Read)
                {
                    Name = "DataStreamHandler.ReadThread",
                    Priority = ThreadPriority.AboveNormal
                };
                _readThread.Start();
                return true;
            }
            catch (Exception ex) 
            {
                return false;
            }
        }

        private void Read()
        {
            byte[] buffer = new byte[READ_BUFFER_SIZE];
            ReadDataBuffer readDataBuffer = new ReadDataBuffer(buffer);
            int readOffset = 0;
            int num2 = 0;
            int readBytesCount = 0;
            bool flag = false;
            int index = 0;
            int num5 = 0;
            while (_networkStream != null && (_isReading || readBytesCount > 0))
            {
                try
                {
                    readBytesCount = 0;
                    if (_networkStream.DataAvailable)
                    {
                        readBytesCount = _networkStream.Read(buffer, readOffset, READ_BUFFER_SIZE - readOffset);
                        num2 = readOffset + readBytesCount;
                        if (readBytesCount > 0)
                        {
                            index = Array.FindIndex(buffer, 0, num2 - readOffset, (byte b) => b != 0);
                            while (index >= 0 && !flag)
                            {
                                readDataBuffer.CurrentIndex = index;
                                readDataBuffer.Skip(1);
                                switch (buffer[index])
                                {
                                    case 7:
                                        HandleData(ref flag, index + READ_BUFFER_SIZE / 2 - 3 > num2, _dataProvider.OnLogData, readDataBuffer);
                                        break;
                                    case 55:
                                        HandleData(ref flag, index + 4 - 3 > num2, _dataProvider.OnAllSignalsRemoved, readDataBuffer);
                                        break;
                                    case 60:
                                        HandleData(ref flag, index + ERROR_MESSAGE_SIZE - 3 > num2, _dataProvider.OnError, readDataBuffer);
                                        break;
                                    case 51:
                                        HandleData(ref flag, index + 4 + SIGNAL_DEFINITION_MESSAGE_SIZE - 3 > num2, _dataProvider.OnSignalDefined, readDataBuffer);
                                        break;
                                    case 54:
                                        HandleData(ref flag, index + 4 - 3 > num2, _dataProvider.OnSignalRemoved, readDataBuffer);
                                        break;
                                    case 56:
                                        HandleData(ref flag, index + 720 - 3 > num2, _dataProvider.OnSignalsEnumerated, readDataBuffer);
                                        break;
                                }
                                if (flag)
                                {
                                    int num7 = num2 - index;
                                    Array.Copy(buffer, index, buffer, 0, num7);
                                    readOffset = num7;
                                    flag = false;
                                    break;
                                }
                                index = Array.FindIndex(buffer, readDataBuffer.CurrentIndex, num2 - readDataBuffer.CurrentIndex, (byte b) => b != 0);
                            }
                            if (index < 0)
                            {
                                readOffset = 0;
                            }
                        }
                        _sleepCounter = 0;
                    }
                    else
                    {
                        _sleepCounter++;
                        if (_sleepCounter == 1000.0)
                        {
                            Thread.Sleep(1);
                            _sleepCounter = 0;
                        }
                    }
                }
                catch (Exception)
                {
                    _isReading = false;
                    Thread.Sleep(1);
                }
            }
        }

        private void Write(byte[] data)
        {
            if (_networkStream == null || data == null)
            {
                return;
            }
            DateTime now = DateTime.Now;
            bool flag = false;
            do
            {
                try
                {
                    _networkStream.Write(data, 0, data.Length);
                    flag = true;
                }
                catch (Exception ex)
                {
                    if ((DateTime.Now - now).TotalMilliseconds < 100.0 && ex.Message.Contains("Unable to write data to the transport connection"))
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    throw;
                }
            }
            while (!flag);
        }

        public void WriteDefineSignal(int channel, int signalNumber, string mechUnitName, int axisNumber, float sampleTime)
        {
            if (channel < 0 || channel > 11)
            {
                throw new ArgumentOutOfRangeException("channel", channel, $"Value of must be between 0 and 11");
            }
            if (axisNumber < 1 || axisNumber > 6)
            {
                throw new ArgumentOutOfRangeException("axisNumber", axisNumber, "Value of must be between 1 and 6");
            }
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData(1);
            dataBuffer.AddData(channel);
            dataBuffer.AddData(signalNumber);
            dataBuffer.AddData(mechUnitName, 40);
            dataBuffer.AddData(axisNumber - 1);
            dataBuffer.AddData(sampleTime);
            Write(dataBuffer.GetData());
        }

        public void WriteRemoveAllSignals()
        {
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData(5);
            Write(dataBuffer.GetData());
        }

        private void HandleData(ref bool flag, bool condition, Action<ReadDataBuffer> dataHandler, ReadDataBuffer readDataBuffer)
        {
            flag = condition;
            if (!flag)
            {
                dataHandler(readDataBuffer);
            }
        }
    }
}
