using MotionMonitor.Enums;
using System.Net.Sockets;
using System.Threading.Channels;

namespace MotionMonitor
{
    public class DataExchangeClient : ISubscriptionDataStreamManager, ILogDataStreamManager
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

        private const int COMMAND_TIMEOUT = 500;
        private const int READ_TIMEOUT = 1000;
        private const int INT32_LENGTH = 4;
        private const int READ_DATA_BUFFER_LENGTH = 7712;
        private const int LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH = 64;
        private const int LOG_ERROR_MESSAGE_LENGTH = 80;

        private Socket _socket;
        private NetworkStream _networkStream;
        private ISubscriptionManager _subscriptionManager;
        private ILogManager _logManager;
        public ISubscriptionManager SubscriptionManager 
        {
            get => _subscriptionManager;
            set => _subscriptionManager = value;
        }
        public ILogManager LogManager
        {
            get => _logManager;
            set => _logManager = value;
        }

        private ManualResetEvent _commandExecuted = new ManualResetEvent(false);



        public bool Init(Socket socket)
        {

            if (_socket == null)
                throw new InvalidOperationException("Socket is null.");

            if (!_socket.Connected)
                throw new InvalidOperationException("Socket is not connected.");

            _socket = socket;

            try
            {
                _networkStream = new NetworkStream(_socket, true) 
                {
                    ReadTimeout = 1000
                };
            }
            catch (Exception ex)
            {
                return false;
            }

            _reading = true;
            _readThread = new Thread(Read)
            {
                Name = "LogSrvStreamHandler.readThread",
                Priority = ThreadPriority.AboveNormal
            };
            _readThread.Start();
            return true;
        }

        private void Write(byte[] data)
        {
            if (_networkStream is null || data is null)
            {
                return;
            }

            DateTime currentDateTime = DateTime.Now;
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
                    if ((DateTime.Now - currentDateTime).TotalMilliseconds < 100.0 && ex.Message.Contains("Unable to write data to the transport connection"))
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    throw;
                }
            }
            while (!flag);
        }
        private void Read()
        {
            byte[] array = new byte[READ_DATA_BUFFER_LENGTH];
            ReadDataBuffer dataBuffer = new(array);
            int offset = 0;
            int num2 = 0;
            int bytesRead = 0;
            int index = 0;
            bool flag = false;

            while (_networkStream != null && (_reading || bytesRead > 0))
            {
                int tryCounter = 0;
                try
                {
                    bytesRead = 0;
                    if (_networkStream.DataAvailable)
                    {
                        bytesRead = _networkStream.Read(array, offset, READ_DATA_BUFFER_LENGTH - offset);
                        num2 = offset + bytesRead;
                        if (bytesRead > 0)
                        {
                            //Edited
                            //num4 = Array.FindIndex(array, 0, num2 - offset, (byte b) => b != 0);
                            index = Array.FindIndex(array, 0, bytesRead, b => b != 0);
                            //------
                            while (index >= 0 && !flag)
                            {
                                dataBuffer.CurrentIndex = index;
                                dataBuffer.Skip(1);
                                switch ((LogMessages)array[index])
                                {
                                    case LogMessages.LogData:
                                        {
                                            if (index + READ_DATA_BUFFER_LENGTH / 2 - 3 > num2)
                                            {
                                                flag = true;
                                                break;
                                            }

                                            int count = dataBuffer.ReadInt();
                                            int channel = dataBuffer.ReadInt();
                                            dataBuffer.ReadInt();
                                            OnLogData(channel, count, dataBuffer);
                                            break;
                                        }
                                    case LogMessages.SubscribtionActivated:
                                        if (index + LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnSubscriptionActivated(dataBuffer);
                                        break;
                                    case LogMessages.LoggingStarted:
                                    case LogMessages.LoggingStopped:
                                    case LogMessages.SubscribtionCanceled:
                                        if (index + INT32_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnSubscriptionCanceled(dataBuffer);
                                        break;
                                    case LogMessages.AllSubscribtionsCanceled:
                                        if (index + INT32_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnAllSubscriptionsCanceled(dataBuffer);
                                        break;
                                    case LogMessages.SubscribtionsEnumerated:
                                        {
                                            if (index + LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH * 12 - 3 > num2)
                                            {
                                                flag = true;
                                                break;
                                            }

                                            OnSubscriptionsEnumerated(dataBuffer);
                                            break;
                                        }
                                    case LogMessages.Error:
                                        if (index + LOG_ERROR_MESSAGE_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnLogError(dataBuffer);
                                        break;
                                }
                                if (flag)
                                {
                                    //Edited
                                    //int num7 = num2 - index;
                                    //Array.Copy(array, index, array, 0, num7);
                                    //offset = num7;
                                    //flag = false;
                                    //break;

                                    offset = num2 - index;
                                    Array.Copy(array, index, array, 0, offset);
                                    flag = false;
                                    break;
                                    //-----
                                }
                                index = Array.FindIndex(array, dataBuffer.CurrentIndex, num2 - dataBuffer.CurrentIndex, (byte b) => b != 0);
                            }
                            if (index < 0)
                            {
                                offset = 0;
                            }
                        }
                        _sleepCounter = 0;
                    }
                    else
                    {
                        if (++tryCounter >= 1000)
                        {
                            Thread.Sleep(1);
                            tryCounter = 0;
                        }
                    }
                }
                catch (Exception)
                {
                    _reading = false;
                    Thread.Sleep(1);
                }
            }
        }

        #region Output messages handlers
        void ILogDataStreamManager.SendStartStopLoggingRequest(LogCommands command, List<int> channels)
        {
            _commandExecuted.Reset();
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData((int)command);
            dataBuffer.AddData(channels.Count);
            foreach (var channel in channels)
            {
                var data = channel < channels.Count ? channel : -1;
                dataBuffer.AddData(data);
            }

            Write(dataBuffer.GetData());
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                //Log.Write(LogLevel.Debug, "TestSignalHandler::StartStopLog", "Timeout!");
                throw new TimeoutException("StartStopLog");
            }
        }

        void ISubscriptionDataStreamManager.SendSubscriptionRequest(RequestDataSubscription dataSubscription)
        {
            _commandExecuted.Reset();
            if (dataSubscription.ChannelNo < 0 || dataSubscription.ChannelNo > 11)
            {
                throw new ArgumentOutOfRangeException("channel", dataSubscription.ChannelNo, string.Format("Value of must be between 0 and {0}", 11));
            }

            if (dataSubscription.AxisNo < 1 || dataSubscription.AxisNo > 6)
            {
                throw new ArgumentOutOfRangeException("axisNumber", dataSubscription.AxisNo, "Value of must be between 1 and 6");
            }
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData(1);
            dataBuffer.AddData(dataSubscription.ChannelNo);
            dataBuffer.AddData(dataSubscription.SignalNo);
            dataBuffer.AddData(dataSubscription.MechUnitName, 40);
            dataBuffer.AddData(dataSubscription.AxisNo - 1);
            dataBuffer.AddData(dataSubscription.SampleTime);
            Write(dataBuffer.GetData());
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                throw new TimeoutException("DefineSignal");
            }
        }

        void ISubscriptionDataStreamManager.SendCancelAllSubscribtionsRequest()
        {
            _commandExecuted.Reset();
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData((int)LogCommands.CancelAllSubscriptions);
            Write(dataBuffer.GetData());
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                throw new TimeoutException("RemoveAllSignals");
            }
        }
        #endregion

        #region Input messages handlers
        public void OnLogData(int channel, int count, ReadDataBuffer dataBuffer)
        {
            var activeSubscription = _subscriptionManager.GetActiveSubscription(channel);
            if (activeSubscription is null)
            {
                return;
            }

            List<double> rawLogDataValues = new(count);
            switch (activeSubscription.Format)
            {
                case LogDataValueFormat.Float:
                    for (int i = 0; i < count; i++)
                    {
                        rawLogDataValues.Add(dataBuffer.ReadFloat());
                    }
                    break;
                case LogDataValueFormat.String:
                case LogDataValueFormat.Undefined:
                    //Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + activeSubscription.Format.ToString() + " and we don't have a way to handle it..");
                    break;
                default:
                    for (int j = 0; j < count; j++)
                    {
                        rawLogDataValues.Add(dataBuffer.ReadInt());
                    }
                    break;

            }
            //List<double> list = new(count);
            if (activeSubscription.Format == LogDataValueFormat.Float)
            {
                for (int i = 0; i < count; i++)
                {
                    list.Add(dataBuffer.ReadFloat());
                }
            }
            else if (activeSubscription.Format == LogDataValueFormat.String || activeSubscription.Format == LogDataValueFormat.Undefined)
            {
                //Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + _definedSignals[channel].Format.ToString() + " and we don't have a way to handle it..");
            }
            else
            {
                for (int j = 0; j < count; j++)
                {
                    logData.Add(dataBuffer.ReadInt());
                }
            }

            ReceiveLogDataObject receiveLogDataObject = _dataLogs[channel];
            List<double> logDataValues = receiveLogDataObject.LoggedDataValues;
            //LogSrvSignalDefinition logSrvSignalDefinition = _definedSignals[channel];
            Trig trig = null;
            if (_trigs.ContainsKey(channel))
            {
                trig = _trigs[channel];
            }

            double sampleTime = activeSubscription.SampleTime;
            double minSampleTime = _subscriptionHandler.GetActiveSubscriptionsMinSampleTime();
            double sampleTimeFactor = sampleTime / minSampleTime;
            int roundedSampleTimeFactor = (int)Math.Round(sampleTimeFactor, 0);
            int loggedDataCount = logDataValues.Count;
            foreach (double rawLogDataValue in rawLogDataValues)
            {
                if (AntiAliasFiltering)
                {
                    if ((int)Math.Round(SampleTime / sampleTime, 0) > 1)
                    {
                        Filter antiAliasFilter = receiveLogDataObject.AntiAliasFilter;
                        antiAliasFilter.Execute(data);
                        if (antiAliasFilter.SampleFinished)
                        {
                            TrigHandler.AddNewSample(loggedData, antiAliasFilter.Value, trig);
                        }
                    }
                    else
                    {
                        if (loggedData.Count == 0)
                        {
                            TrigHandler.AddNewSample(loggedData, data, trig);
                            TrigHandler.AddNewSample(loggedData, data, trig);
                        }
                        TrigHandler.AddNewSample(loggedData, data, trig);
                    }
                    continue;
                }
   
                if (Math.Abs(roundedSampleTimeFactor - sampleTimeFactor) > 0.1)
                {
                    int num3 = 2;
                    while (Math.Abs(sampleTimeFactor * roundedSampleTimeFactor - Math.Floor(sampleTimeFactor * roundedSampleTimeFactor)) > 0.1)
                    {
                        num3++;
                    }

                    int receivedLogData = receiveLogDataObject.ReceivedLogData + 1 > num3 ? 1 : receiveLogDataObject.ReceivedLogData + 1;
                    receiveLogDataObject.ReceivedLogData = receivedLogData;
                    int num4 = logDataValues.Count % (int)(num3 * sampleTimeFactor);
                    roundedSampleTimeFactor = (int)Math.Floor(receivedLogData * sampleTimeFactor);
                    roundedSampleTimeFactor -= num4 > 0 ? num4 : 0; 
                }
                if (roundedSampleTimeFactor <= 0)
                {
                    continue;
                }

                double logDataValue = logDataValues.Count > 0 ? logDataValues[^1] : rawLogDataValue;
                for (int k = 0; k < roundedSampleTimeFactor; k++)
                {
                    if (ZeroOrderHold)
                    {
                        TrigHandler.AddNewSample(logDataValues, rawLogDataValue, trig);
                    }
                    else
                    {
                        TrigHandler.AddNewSample(logDataValues, logDataValue + (rawLogDataValue - logDataValue) / roundedSampleTimeFactor * (k + 1), trig);
                    }
                }
            }
            receiveLogDataObject.LoggedSamples += logDataValues.Count - loggedDataCount;
            rawLogDataValues.Clear();
            if (MaxLogTime > 0)
            {
                int num6 = (int)((double)MaxLogTime / GetSampleTime()) + 1;
                if (logDataValues.Count > num6)
                {
                    int num7 = logDataValues.Count - num6;
                    foreach (ReceiveLogDataObject value in _dataLogs.Values)
                    {
                        if (value.LoggedData != null && value.LoggedData.Count > num7)
                        {
                            for (int l = 0; l < num7; l++)
                            {
                                value.LoggedData.RemoveAt(0);
                            }
                        }
                    }
                }
            }
            if (this.TestSignalRecived != null && channel == _lastChannel)
            {
                this.TestSignalRecived(this, new EventArgs());
            }
        }

        public void OnLogStarted()
        {
            _commandExecuted.Set();
        }

        public void OnLogStoped()
        {
            _commandExecuted.Set();
        }

        public void OnSubscriptionActivated(ReadDataBuffer dataBuffer)
        {
            var activeDataSubscribtion = ActiveDataSubscription.BuildDataSubscription(dataBuffer);
            _subscriptionManager.AddActiveSubscription(activeDataSubscribtion);
            _commandExecuted.Set();
        }

        public void OnSubscriptionCanceled(ReadDataBuffer dataBuffer)
        {
            int status = dataBuffer.ReadInt(); ;
            _commandExecuted.Set();
        }

        public void OnAllSubscriptionsCanceled(ReadDataBuffer dataBuffer)
        {
            int status = dataBuffer.ReadInt();
            _subscriptionManager.RemoveAllActiveSubscriptions();
            _commandExecuted.Set();
        }

        public void OnSubscriptionsEnumerated(ReadDataBuffer dataBuffer)
        {
            List<ActiveDataSubscription> activeDataSubscriptions = new();
            for (int i = 0; i < 12; i++)
            {
                activeDataSubscriptions[i] = ActiveDataSubscription.BuildDataSubscription(dataBuffer);
            }
            _subscriptionManager.EnumerateActiveSubscriptions(activeDataSubscriptions);
            _commandExecuted.Set();
        }

        public void OnLogError(ReadDataBuffer dataBuffer)
        {
            var errorMessage = dataBuffer.ReadString(LOG_ERROR_MESSAGE_LENGTH);
            EventHandler<NotifyEventArgs<string>> eventHandler = this.Notification;

            if (eventHandler != null)
            {
                eventHandler(this, new NotifyEventArgs<string>(errorMessage));
            }
        }
        #endregion

    }

    public interface ISubscriptionDataStreamManager
    {
        public ISubscriptionManager SubscriptionManager { get; set; }
        public void SendSubscriptionRequest(RequestDataSubscription dataSubscription);
        public void SendCancelAllSubscribtionsRequest();
    }

    public interface ILogDataStreamManager
    {
        public ILogManager LogManager { get; set; }
        public void SendStartStopLoggingRequest(LogCommands command, List<int> channels);
    }
}
