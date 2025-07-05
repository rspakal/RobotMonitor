using MotionMonitor.Enums;

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

        private const int COMMAND_TIMEOUT = 500;
        private const int INT32_LENGTH = 4;
        private const int READ_DATA_BUFFER_LENGTH = 7712;
        private const int LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH = 64;
        private const int LOG_ERROR_MESSAGE_LENGTH = 80;

        private ManualResetEvent _commandExecuted;
        private IDataSubscriptionsHandler _subscriptionHandler;
        public DataStreamHandler(IDataSubscriptionsHandler subscriptionHandler)
        {
            _subscriptionHandler = subscriptionHandler;
            _commandExecuted = new ManualResetEvent(false);
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
        public void SendCancelAllSubscribtionsRequest()
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
        public void SendSubscriptionRequest(DataSubscription dataSubscription)
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

        #region Log messages handlers
        public void OnSubscriptionActivated(ReadDataBuffer dataBuffer)
        {
            var activeDataSubscribtion = ActiveDataSubscription.BuildDataSubscription(dataBuffer);
            _subscriptionHandler.AddActiveSubscription(activeDataSubscribtion);
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
            _subscriptionHandler.RemoveAllActiveSubscriptions();
            _commandExecuted.Set();
        }
        public void OnSubscriptionsEnumerated(ReadDataBuffer dataBuffer)
        {
            List<ActiveDataSubscription> activeDataSubscriptions = new();
            for (int i = 0; i < 12; i++)
            {
                activeDataSubscriptions[i] = ActiveDataSubscription.BuildDataSubscription(dataBuffer);
            }
            _subscriptionHandler.EnumerateActiveSubscriptions(activeDataSubscriptions);
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
                                switch (array[index])
                                {
                                    case 7:
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
                                    case 55:
                                        if (index + INT32_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnAllSubscriptionsCanceled(dataBuffer);
                                        break;
                                    case 60:
                                        if (index + LOG_ERROR_MESSAGE_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnLogError(dataBuffer);
                                        break;
                                    case 51:
                                        if (index + LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnSubscriptionActivated(dataBuffer);
                                        break;
                                    case 54:
                                        if (index + INT32_LENGTH - 3 > num2)
                                        {
                                            flag = true;
                                            break;
                                        }

                                        OnSubscriptionCanceled(dataBuffer);
                                        break;
                                    case 56:
                                        {
                                            if (index + LOG_SUBSCRIBTION_ACTIVATED_MESSAGE_LENGTH * 12 - 3 > num2)
                                            {
                                                flag = true;
                                                break;
                                            }

                                            OnSubscriptionsEnumerated(dataBuffer);
                                            break;
                                        }
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
                        _sleepCounter++;
                        if (_sleepCounter == 1000)
                        {
                            Thread.Sleep(1);
                            _sleepCounter = 0;
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

        protected override void OnLogData(int channel, int count, ReadDataBuffer dataBuffer)
        {
            var activeSubscription = _subscriptionHandler.GetActiveSubscription(channel);
            if (activeSubscription is null)
            {
                return;
            }

            List<double> logData = new(count);
            switch (activeSubscription.Format)
            {
                case LogDataValueFormat.Float:
                    for (int i = 0; i < count; i++)
                    {
                        logData.Add(dataBuffer.ReadFloat());
                    }
                    break;
                case LogDataValueFormat.String:
                case LogDataValueFormat.Undefined:
                    Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + activeSubscription.Format.ToString() + " and we don't have a way to handle it..");
                    break;
                default:
                    for (int j = 0; j < count; j++)
                    {
                        logData.Add(dataBuffer.ReadInt());
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
                Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + _definedSignals[channel].Format.ToString() + " and we don't have a way to handle it..");
            }
            else
            {
                for (int j = 0; j < count; j++)
                {
                    list.Add(dataBuffer.ReadInt());
                }
            }

            ReceiveLogDataObject receiveLogDataObject = _dataLogs[channel];
            List<double> loggedData = receiveLogDataObject.LoggedData;
            LogSrvSignalDefinition logSrvSignalDefinition = _definedSignals[channel];
            Trig trig = null;
            if (_trigs.ContainsKey(channel))
            {
                trig = _trigs[channel];
            }

            double sampleTime = logSrvSignalDefinition.SampleTime;
            int count2 = loggedData.Count;
            foreach (double item in list)
            {
                if (AntiAliasFiltering)
                {
                    if ((int)Math.Round(SampleTime / sampleTime, 0) > 1)
                    {
                        Filter antiAliasFilter = receiveLogDataObject.AntiAliasFilter;
                        antiAliasFilter.Execute(item);
                        if (antiAliasFilter.SampleFinished)
                        {
                            TrigHandler.AddNewSample(loggedData, antiAliasFilter.Value, trig);
                        }
                    }
                    else
                    {
                        if (loggedData.Count == 0)
                        {
                            TrigHandler.AddNewSample(loggedData, item, trig);
                            TrigHandler.AddNewSample(loggedData, item, trig);
                        }
                        TrigHandler.AddNewSample(loggedData, item, trig);
                    }
                    continue;
                }
                double minSampleTime = GetMinSampleTime();
                double num = sampleTime / minSampleTime;
                int num2 = (int)Math.Round(num, 0);
                if (Math.Abs((double)num2 - num) > 0.1)
                {
                    int num3 = 2;
                    while (Math.Abs(num * (double)num2 - Math.Floor(num * (double)num2)) > 0.1)
                    {
                        num3++;
                    }
                    int receivedLogData = receiveLogDataObject.ReceivedLogData;
                    receivedLogData++;
                    if (receivedLogData > num3)
                    {
                        receivedLogData = 1;
                    }
                    receiveLogDataObject.ReceivedLogData = receivedLogData;
                    int num4 = loggedData.Count % (int)((double)num3 * num);
                    num2 = (int)Math.Floor((double)receivedLogData * num);
                    if (num4 > 0)
                    {
                        num2 -= num4;
                    }
                }
                if (num2 <= 0)
                {
                    continue;
                }
                double num5 = item;
                if (loggedData.Count > 0)
                {
                    num5 = loggedData[loggedData.Count - 1];
                }
                for (int k = 0; k < num2; k++)
                {
                    if (ZeroOrderHold)
                    {
                        TrigHandler.AddNewSample(loggedData, item, trig);
                    }
                    else
                    {
                        TrigHandler.AddNewSample(loggedData, num5 + (item - num5) / (double)num2 * (double)(k + 1), trig);
                    }
                }
            }
            receiveLogDataObject.LoggedSamples += loggedData.Count - count2;
            list.Clear();
            if (MaxLogTime > 0)
            {
                int num6 = (int)((double)MaxLogTime / GetSampleTime()) + 1;
                if (loggedData.Count > num6)
                {
                    int num7 = loggedData.Count - num6;
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


    }
}
