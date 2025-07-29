using MotionMonitor.Enums;
using System;
using System.Net.Sockets;

namespace MotionMonitor
{
    public class DataExchangeClient : ISubscriptionDataStreamManager, ILogDataStreamManager
    {
        private enum DataMessage
        {
            LogData = 7,
            SubscriptionActivated = 51,
            LoggingStarted = 52,
            LoggingStopped = 53,
            SubscriptionCanceled = 54,
            AllSubscriptionsCanceled = 55,
            SubscriptionsEnumerated = 56,
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

        private CancellationTokenSource _readTaskCancellationTokenSource = new CancellationTokenSource();
        private Dictionary<DataMessage, Action<ReadDataBuffer>> LogDataHandlers;
        private bool _reading;
        private ManualResetEvent _commandExecuted = new ManualResetEvent(false);

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

        public DataExchangeClient()
        {
            LogDataHandlers = [];
            LogDataHandlers.Add(DataMessage.LogData, OnLogData);
            LogDataHandlers.Add(DataMessage.SubscriptionCanceled, OnSubscriptionCanceled);
            LogDataHandlers.Add(DataMessage.SubscriptionActivated, OnSubscriptionActivated);
            LogDataHandlers.Add(DataMessage.AllSubscriptionsCanceled, OnAllSubscriptionsCanceled);
            LogDataHandlers.Add(DataMessage.LoggingStarted, OnLogStarted);
            LogDataHandlers.Add(DataMessage.LoggingStopped, OnLogStoped);
            LogDataHandlers.Add(DataMessage.Error, OnLogError);
        }


        public bool Init(Socket socket)
        {
            if (socket == null)
                throw new InvalidOperationException("Socket is null.");

            if (!socket.Connected)
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
            StartRead();
            return true;
        }

        private void StartRead()
        {
            _readTaskCancellationTokenSource ??= new CancellationTokenSource();
            _reading = true;
            var readingTask = Read(_readTaskCancellationTokenSource.Token);
        }

        private void StopRead()
        {
            _reading = false;
            _readTaskCancellationTokenSource?.Cancel();
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

        private async Task Read(CancellationToken token)
        {
            byte[] buffer = new byte[READ_DATA_BUFFER_LENGTH];
            ReadDataBuffer dataBuffer = new(buffer);
            int offset = 0;
            int bytesAvailable = 0;
            int bytesRead = 0;
            int index = 0;
            bool flag = false;
            int sleepCounter = 0;
            while (_networkStream != null && (_reading || bytesRead > 0) && !token.IsCancellationRequested)
            {
                try
                {
                    bytesRead = 0;
                    if (!_networkStream.DataAvailable)
                    {
                        if (++sleepCounter >= 1000)
                        {
                            await Task.Delay(1);
                            sleepCounter = 0;
                            StopRead();
                        }
                        continue;
                    }

                    sleepCounter = 0;
                    bytesRead = _networkStream.Read(buffer, offset, READ_DATA_BUFFER_LENGTH - offset);
                    bytesAvailable = offset + bytesRead;
                    if(bytesRead <= 0)
                    {
                        continue;
                    }

                    index = Array.FindIndex(buffer, 0, bytesRead, b => b != 0);
                    if (index < 0)
                    {
                        offset = 0;
                    }

                    while (index >= 0)
                    {
                        dataBuffer.CurrentIndex = index;
                        dataBuffer.Skip(1);
                        switch ((LogMessages)buffer[index])
                        {
                            case LogMessages.LogData:

                                if (index + READ_DATA_BUFFER_LENGTH / 2 - 3 > bytesAvailable)
                                {
                                    flag = true;
                                    break;
                                }

                                OnLogData(dataBuffer);
                                break;
                            case LogMessages.SubscribtionActivated:
                                if (index + LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH - 3 > bytesAvailable)
                                {
                                    flag = true;
                                    break;
                                }

                                OnSubscriptionActivated(dataBuffer);
                                break;
                            case LogMessages.LoggingStarted:
                            case LogMessages.LoggingStopped:
                            case LogMessages.SubscribtionCanceled:
                                if (index + INT32_LENGTH - 3 > bytesAvailable)
                                {
                                    flag = true;
                                    break;
                                }

                                OnSubscriptionCanceled(dataBuffer);
                                break;
                            case LogMessages.AllSubscribtionsCanceled:
                                flag = HandleReadData(index, bytesAvailable, INT32_LENGTH, OnAllSubscriptionsCanceled, dataBuffer);
                                break;
                            case LogMessages.SubscribtionsEnumerated:
                                flag = HandleReadData(index, bytesAvailable, LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH * 12, OnSubscriptionsEnumerated, dataBuffer);
                                break;
                            case LogMessages.Error:
                                flag = HandleReadData(index, bytesAvailable, LOG_ERROR_MESSAGE_LENGTH, OnLogError, dataBuffer);
                                break;
                        }
                        if (flag)
                        {
                            offset = bytesAvailable - index;
                            Array.Copy(buffer, index, buffer, 0, offset);
                            flag = false;
                            break;
                        }

                        index = Array.FindIndex(buffer, dataBuffer.CurrentIndex, bytesAvailable - dataBuffer.CurrentIndex, b => b != 0);
                    }

                }
                catch (Exception)
                {
                    _reading = false;
                    await Task.Delay(1);
                }
            }
        }
        private bool HandleReadData(int index, int bytesAvailable,  int messageSize, Action<ReadDataBuffer> dataHandler, ReadDataBuffer dataBuffer)
        {
            if (index + messageSize - 3 > bytesAvailable)
            {
                return true;
            }

            dataHandler(dataBuffer);
            return false;
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

        void ISubscriptionDataStreamManager.SendSubscriptionRequest(KeyValuePair<int, RequestDataSubscription> dataSubscription)
        {
            _commandExecuted.Reset();
            if (dataSubscription.Key < 0 || dataSubscription.Key > 11)
            {
                throw new ArgumentOutOfRangeException("channel", dataSubscription.Key, string.Format("Value of must be between 0 and {0}", 11));
            }

            if (dataSubscription.Value.AxisNo < 1 || dataSubscription.Value.AxisNo > 6)
            {
                throw new ArgumentOutOfRangeException("axisNumber", dataSubscription.Value.AxisNo, "Value of must be between 1 and 6");
            }
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData(1);
            dataBuffer.AddData(dataSubscription.Key);
            dataBuffer.AddData(dataSubscription.Value.SignalNo);
            dataBuffer.AddData(dataSubscription.Value.MechUnitName, 40);
            dataBuffer.AddData(dataSubscription.Value.AxisNo - 1);
            dataBuffer.AddData(dataSubscription.Value.SampleTime);
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
        //public void OnLogData(int channel, int count, ReadDataBuffer dataBuffer)
        //{
        //    var activeSubscription = _subscriptionManager.GetActiveSubscription(channel);
        //    if (activeSubscription is null)
        //    {
        //        return;
        //    }

        //    List<double> rawLogDataValues = new(count);
        //    switch (activeSubscription.Format)
        //    {
        //        case LogDataValueFormat.Float:
        //            for (int i = 0; i < count; i++)
        //            {
        //                rawLogDataValues.Add(dataBuffer.ReadFloat());
        //            }
        //            break;
        //        case LogDataValueFormat.String:
        //        case LogDataValueFormat.Undefined:
        //            //Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + activeSubscription.Format.ToString() + " and we don't have a way to handle it..");
        //            break;
        //        default:
        //            for (int j = 0; j < count; j++)
        //            {
        //                rawLogDataValues.Add(dataBuffer.ReadInt());
        //            }
        //            break;

        //    }

        //    #region
        //    //List<double> list = new(count);
        //    if (activeSubscription.Format == LogDataValueFormat.Float)
        //    {
        //        for (int i = 0; i < count; i++)
        //        {
        //            list.Add(dataBuffer.ReadFloat());
        //        }
        //    }
        //    else if (activeSubscription.Format == LogDataValueFormat.String || activeSubscription.Format == LogDataValueFormat.Undefined)
        //    {
        //        //Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + _definedSignals[channel].Format.ToString() + " and we don't have a way to handle it..");
        //    }
        //    else
        //    {
        //        for (int j = 0; j < count; j++)
        //        {
        //            logData.Add(dataBuffer.ReadInt());
        //        }
        //    }
        //    #endregion
        //    var motionDataLog = _logManager.GetMotionDataLog(channel);
        //    if(motionDataLog is null)
        //    {
        //        return;
        //    }

        //    var logDataValues = motionDataLog.LoggedDataValues;

        //    //Trig trig = null;
        //    //if (_trigs.ContainsKey(channel))
        //    //{
        //    //    trig = _trigs[channel];
        //    //}

        //    var subscriptionSampleTime = activeSubscription.SampleTime;
        //    double minSubscriptionSampleTime = _subscriptionManager.GetActiveSubscriptionsMinSampleTime();
        //    double sampleTimeFactor = subscriptionSampleTime / minSubscriptionSampleTime;
        //    int roundedSampleTimeFactor = (int)Math.Round(sampleTimeFactor, 0);
        //    int loggedDataCount = logDataValues.Count;
        //    foreach (double rawLogDataValue in rawLogDataValues)
        //    {
        //        if (AntiAliasFiltering)
        //        {
        //            if ((int)Math.Round(SampleTime / sampleTime, 0) > 1)
        //            {
        //                Filter antiAliasFilter = receiveLogDataObject.AntiAliasFilter;
        //                antiAliasFilter.Execute(data);
        //                if (antiAliasFilter.SampleFinished)
        //                {
        //                    TrigHandler.AddNewSample(loggedData, antiAliasFilter.Value, trig);
        //                }
        //            }
        //            else
        //            {
        //                if (loggedData.Count == 0)
        //                {
        //                    TrigHandler.AddNewSample(loggedData, data, trig);
        //                    TrigHandler.AddNewSample(loggedData, data, trig);
        //                }
        //                TrigHandler.AddNewSample(loggedData, data, trig);
        //            }
        //            continue;
        //        }

        //        if (Math.Abs(roundedSampleTimeFactor - sampleTimeFactor) > 0.1)
        //        {
        //            int num3 = 2;
        //            while (Math.Abs(sampleTimeFactor * roundedSampleTimeFactor - Math.Floor(sampleTimeFactor * roundedSampleTimeFactor)) > 0.1)
        //            {
        //                num3++;
        //            }

        //            int receivedLogData = receiveLogDataObject.ReceivedLogData + 1 > num3 ? 1 : receiveLogDataObject.ReceivedLogData + 1;
        //            receiveLogDataObject.ReceivedLogData = receivedLogData;
        //            int num4 = logDataValues.Count % (int)(num3 * sampleTimeFactor);
        //            roundedSampleTimeFactor = (int)Math.Floor(receivedLogData * sampleTimeFactor);
        //            roundedSampleTimeFactor -= num4 > 0 ? num4 : 0; 
        //        }
        //        if (roundedSampleTimeFactor <= 0)
        //        {
        //            continue;
        //        }

        //        double logDataValue = logDataValues.Count > 0 ? logDataValues[^1] : rawLogDataValue;
        //        for (int k = 0; k < roundedSampleTimeFactor; k++)
        //        {
        //            if (ZeroOrderHold)
        //            {
        //                TrigHandler.AddNewSample(logDataValues, rawLogDataValue, trig);
        //            }
        //            else
        //            {
        //                TrigHandler.AddNewSample(logDataValues, logDataValue + (rawLogDataValue - logDataValue) / roundedSampleTimeFactor * (k + 1), trig);
        //            }
        //        }
        //    }
        //    receiveLogDataObject.LoggedSamples += logDataValues.Count - loggedDataCount;
        //    rawLogDataValues.Clear();
        //    if (MaxLogTime > 0)
        //    {
        //        int num6 = (int)((double)MaxLogTime / GetSampleTime()) + 1;
        //        if (logDataValues.Count > num6)
        //        {
        //            int num7 = logDataValues.Count - num6;
        //            foreach (ReceiveLogDataObject value in _dataLogs.Values)
        //            {
        //                if (value.LoggedData != null && value.LoggedData.Count > num7)
        //                {
        //                    for (int l = 0; l < num7; l++)
        //                    {
        //                        value.LoggedData.RemoveAt(0);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    if (this.TestSignalRecived != null && channel == _lastChannel)
        //    {
        //        this.TestSignalRecived(this, new EventArgs());
        //    }
        //}

        public void OnLogData(ReadDataBuffer dataBuffer)
        {
            _logManager.HandleMotionDataLog(dataBuffer);
        }
        public void OnLogStarted(ReadDataBuffer dataBuffer)
        {
            _commandExecuted.Set();
        }

        public void OnLogStoped(ReadDataBuffer dataBuffer)
        {
            _commandExecuted.Set();
        }

        public void OnSubscriptionActivated(ReadDataBuffer dataBuffer)
        {
            var channelNo = dataBuffer.ReadInt();
            var signalNo = dataBuffer.ReadInt();
            var mechUnitName = dataBuffer.ReadString(DataSubscription.MECH_UNIT_NAME_LENGTH);
            var axisNo = dataBuffer.ReadInt();
            var format = (LogDataValueFormat)dataBuffer.ReadInt();
            var sampleTime = dataBuffer.ReadFloat();
            var blockSize = dataBuffer.ReadInt();
            var enabled = signalNo != 0;
            _subscriptionManager.AddActiveSubscription(channelNo, new ActiveDataSubscription(signalNo, mechUnitName, axisNo, sampleTime, format, blockSize, enabled));
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
            Dictionary<int,ActiveDataSubscription> activeDataSubscriptions = [];
            for (int i = 0; i < 12; i++)
            {
                activeDataSubscriptions[i] = ActiveDataSubscription.BuildActiveDataSubscription(dataBuffer);
            }
            _subscriptionManager.EnumerateActiveSubscriptions(activeDataSubscriptions);
            _commandExecuted.Set();
        }

        public void OnLogError(ReadDataBuffer dataBuffer)
        {
            var errorMessage = dataBuffer.ReadString(LOG_ERROR_MESSAGE_LENGTH);
            //EventHandler<NotifyEventArgs<string>> eventHandler = this.Notification;

            //if (eventHandler != null)
            //{
            //    eventHandler(this, new NotifyEventArgs<string>(errorMessage));
            //}
        }
        #endregion

    }

    public interface ISubscriptionDataStreamManager
    {
        public ISubscriptionManager SubscriptionManager { get; set; }
        public void SendSubscriptionRequest(KeyValuePair<int, RequestDataSubscription> dataSubscription);
        public void SendCancelAllSubscribtionsRequest();
    }

    public interface ILogDataStreamManager
    {
        public ILogManager LogManager { get; set; }
        public void SendStartStopLoggingRequest(LogCommands command, List<int> channels);
    }
}
