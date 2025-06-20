using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class DataHandler
    {
        private const int MAX_ERROR_MESSAGE_SIZE = 80;
        private ManualResetEvent _commandExecuted;
        private MotionDataProvider _dataProvider;
        private int _lastChannel;
        public DataHandler(MotionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }
        public void OnLogData(ReadDataBuffer dataBuffer)
        {
            int count = dataBuffer.ReadInt();
            int channel = dataBuffer.ReadInt();
            dataBuffer.ReadInt();

            if (!_definedSignals.ContainsKey(channel))
            {
                return;
            }

            List<double> list = new(count);
            if (_definedSignals[channel].Format == LogSrvSignalDefinition.LogSvrValueFormat.Float)
            {
                for (int i = 0; i < count; i++)
                {
                    list.Add(dataBuffer.ReadFloat());
                }
            }
            else if (_definedSignals[channel].Format == LogSrvSignalDefinition.LogSvrValueFormat.String || _definedSignals[channel].Format == LogSrvSignalDefinition.LogSvrValueFormat.Undefined)
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
        public void OnSignalDefined(ReadDataBuffer dataBuffer)
        {
            var channel = dataBuffer.ReadInt();
            var signal = Signal.BuildSignal(dataBuffer);
            lock (dataProvider.SubscribedSignals)
            {
                dataProvider.SubscribedSignals.Add(channel, signal);
                _lastChannel = channel;
            }
            _commandExecuted.Set();
        }
        public void OnSignalRemoved(ReadDataBuffer dataBuffer)
        {
            var status = dataBuffer.ReadInt();
            _commandExecuted.Set();
        }
        public void OnAllSignalsRemoved(ReadDataBuffer dataBuffer)
        {
            int status = dataBuffer.ReadInt();
            lock (dataProvider.SubscribedSignals)
            {
                dataProvider.SubscribedSignals.Clear();
            }
            //lock (_trigs)
            //{
            //    _trigs.Clear();
            //}
            _commandExecuted.Set();
        }
        public void OnSignalsEnumerated(ReadDataBuffer dataBuffer)
        {
            LogSrvSignalDefinition[] signals = new LogSrvSignalDefinition[12];
            for (int i = 0; i < 12; i++)
            {
                signals[i] = new LogSrvSignalDefinition(dataBuffer);
            }

            lock (dataProvider.SubscribedSignals)
            {
                dataProvider.SubscribedSignals.Clear();
                for (int i = 0; i < signals.Length; i++)
                {
                    if (signals[i].Enabled)
                    {
                        dataProvider.SubscribedSignals.Add(i, signals[i]);
                    }
                }
            }
            _commandExecuted.Set();
        }
        public void OnError(ReadDataBuffer dataBuffer)
        {
            var error = dataBuffer.ReadString(MAX_ERROR_MESSAGE_SIZE);
        }

    }
}
