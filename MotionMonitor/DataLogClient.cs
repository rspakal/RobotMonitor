using MotionMonitor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MotionMonitor
{
    internal class DataLogClient : ILogManager
    {
        private Dictionary<int, ActiveDataSubscription> _activeSubscriptions;
        private Dictionary<int, MotionData> _motionDataLogs;
        private double _sampleTime = 0;
        private bool _antiAliasFiltering = true;
        private bool _zeroOrderHold = false;


        private ILogDataStreamManager _logDataStreamManager;

        public DataLogClient(ILogDataStreamManager logDataStreamManager, Dictionary<int, ActiveDataSubscription> activeSubscriptions)
        {
            _logDataStreamManager = logDataStreamManager;
            _logDataStreamManager.LogManager = this;
            _activeSubscriptions = activeSubscriptions;
            _motionDataLogs = new();

            double sampleTime = DataSubscription.SAMPLE_TIME;
            if (_sampleTime <= 0.0)
            {
                _sampleTime = sampleTime;
            }

            _sampleTime = (_sampleTime / sampleTime) * (sampleTime + 5E-07);
            _sampleTime *= 1000.0;
        }

        public bool Init()
        {
            try
            {
                PrepareMotionDataLogBuffer();
                StartStopLogging(LogCommands.StartLog);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PrepareMotionDataLogBuffer()
        {
            _motionDataLogs ??= _activeSubscriptions.ToDictionary(s => s.Key, s => new MotionData());
            //foreach (var activeSubscription in _activeSubscriptions)
            //{
            //    _motionDataLogs.Ad
            //    MotionDataLog motionDataLog = new();
            //    if (_antiAliasFiltering)
            //    {
            //        double activeSubscriptionSampleTime = activeSubscription.SampleTime;
            //        int num = (int)Math.Round(_sampleTime / activeSubscriptionSampleTime, 0);
            //        Filter antiAliasFilter = null;
            //        if (num > 1)
            //        {
            //            antiAliasFilter = new Cheby1Filter(num);
            //        }
            //        motionDataLog.AntiAliasFilter = antiAliasFilter;
            //    }
            //    motionDataLog.AxisNo = activeSubscription.AxisNo;
            //    motionDataLog.SignalNo = activeSubscription.SignalNo;
            //    motionDataLog.Values = new();
            //    _motionDataLogs.Add(activeSubscription.ChannelNo, motionDataLog);
            //}
        }

        private void StartStopLogging(LogCommands command)
        {
            var channels = _activeSubscriptions.Keys.ToList();
            try
            {
                _logDataStreamManager.SendStartStopLoggingRequest(command, channels);
            }
            catch 
            {
                throw;
            }
        }

        void ILogManager.HandleMotionDataLog(ReadDataBuffer dataBuffer)
        {
            int count = dataBuffer.ReadInt();
            int channelNo= dataBuffer.ReadInt();
            dataBuffer.ReadInt();
            List<double> rawMotionDataValues;

            var subscription = _activeSubscriptions[channelNo];
            if (subscription is null)
            {
                return;
            }
            var subscriptionSampleTime = subscription.SampleTime;
            var minSubscriptionSampleTime =_activeSubscriptions?.Select(v => v.Value.SampleTime).DefaultIfEmpty(-1).Min() ?? -1;
            var sampleTimeFactor = subscriptionSampleTime / minSubscriptionSampleTime;
            var roundedSampleTimeFactor = (int)Math.Round(sampleTimeFactor, 0);

            //var motionData = _motionDataLogs.FirstOrDefault(l => l.ChannelNo == channelNo);
            if (!_motionDataLogs.TryGetValue(channelNo, out MotionData motionData) )
            {
                return;
            }

            var motionDataValues = motionData.Values;
            var motionDataValuesCount = motionDataValues.Count;
            rawMotionDataValues = new(count);
            switch (subscription.Format)
            {
                case LogDataValueFormat.Float:
                    for (int i = 0; i < count; i++)
                    {
                        rawMotionDataValues.Add(dataBuffer.ReadFloat());
                    }
                    break;
                case LogDataValueFormat.String:
                case LogDataValueFormat.Undefined:
                    //Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + activeSubscription.Format.ToString() + " and we don't have a way to handle it..");
                    break;
                default:
                    for (int j = 0; j < count; j++)
                    {
                        rawMotionDataValues.Add(dataBuffer.ReadInt());
                    }
                    break;
            }

            foreach (var rawMotionDataValue in rawMotionDataValues)
            {
                if (_antiAliasFiltering)
                {
                    if ((int)Math.Round(_sampleTime / subscriptionSampleTime, 0) > 1)
                    {
                        Filter antiAliasFilter = motionData.AntiAliasFilter;
                        antiAliasFilter.Execute(rawMotionDataValue);
                        if (antiAliasFilter.SampleFinished)
                        {
                            motionData.Values.Add(antiAliasFilter.Value);
                        }
                    }
                    else
                    {
                        motionData.Values.Add(rawMotionDataValue);
                    }
                    continue;
                }
                //if (Math.Abs(roundedSampleTimeFactor - sampleTimeFactor) > 0.1)
                //{
                //    int num3 = 2;
                //    while (Math.Abs(sampleTimeFactor * roundedSampleTimeFactor - Math.Floor(sampleTimeFactor * roundedSampleTimeFactor)) > 0.1)
                //    {
                //        num3++;
                //    }

                //    int receivedLogData = receiveLogDataObject.ReceivedLogData + 1 > num3 ? 1 : receiveLogDataObject.ReceivedLogData + 1;
                //    receiveLogDataObject.ReceivedLogData = receivedLogData;
                //    int num4 = logDataValues.Count % (int)(num3 * sampleTimeFactor);
                //    roundedSampleTimeFactor = (int)Math.Floor(receivedLogData * sampleTimeFactor);
                //    roundedSampleTimeFactor -= num4 > 0 ? num4 : 0;
                //}
                //if (roundedSampleTimeFactor <= 0)
                //{
                //    continue;
                //}
                #region Interpolation
                double lastMotionDataValue = motionData.Values.Count > 0 ? motionData.Values.LastOrDefault() : rawMotionDataValue;
                for (int k = 0; k < roundedSampleTimeFactor; k++)
                {
                    if (_zeroOrderHold)
                    {
                        //TrigHandler.AddNewSample(logDataValues, rawLogDataValue, trig);
                        motionData.Values.Add(rawMotionDataValue);
                    }
                    else
                    {
                        var interpolationStep = (rawMotionDataValue - lastMotionDataValue) / roundedSampleTimeFactor;
                        //TrigHandler.AddNewSample(logDataValues, lastMotionDataValue + interpolationStep * (k + 1), trig);
                        motionData.Values.Add(lastMotionDataValue + interpolationStep * (k + 1));
                    }
                }
                #endregion
            }
        }

        async Task HandleMotionData()
        {
            while (true)
            {
                if (_motionDataLogs[11].Values.Count >= 10000)
                {
                    var values = _motionDataLogs[11].Values;
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }
    }

    public interface ILogManager
    {
        void HandleMotionDataLog(ReadDataBuffer dataBuffer);
    }
}
