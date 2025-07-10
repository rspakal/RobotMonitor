using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MotionMonitor.Enums;

namespace MotionMonitor
{
    internal class LogDataClient : ILogManager
    {
        private List<ReceiveLogDataObject>? _dataLogs;
        private List<ActiveDataSubscription> _activeSubscriptions;


        private ILogDataStreamManager _logDataStreamManager;

        public LogDataClient(ILogDataStreamManager logDataStreamManager, List<ActiveDataSubscription> activeSubscriptions)
        {
            _logDataStreamManager = logDataStreamManager;
            _logDataStreamManager.LogManager = this;
            _activeSubscriptions = activeSubscriptions;
        }

        public bool Init()
        {
            PrepareLogDataBuffer(_activeSubscriptions);
            StartStopLogging(LogCommands.StartLog);
        }

        private void PrepareLogDataBuffer(List<ActiveDataSubscription> activeSubscriptions)
        {
            _dataLogs ??= new List<ReceiveLogDataObject>();
            foreach (var subscription in activeSubscriptions)
            {
                ReceiveLogDataObject receiveLogDataObject = new();
                if (AntiAliasFiltering)
                {
                    double sampleTime = subscription.SampleTime;
                    int num = (int)Math.Round(SampleTime / sampleTime, 0);
                    Filter antiAliasFilter = null;
                    if (num > 1)
                    {
                        antiAliasFilter = new Cheby1Filter(num);
                    }
                    receiveLogDataObject.AntiAliasFilter = antiAliasFilter;
                }
                receiveLogDataObject.ChannelNo = subscription.ChannelNo;
                receiveLogDataObject.LoggedDataValues = new();
                receiveLogDataObject.LoggedSamples = 0;
                receiveLogDataObject.ReceivedLogData = 0;
                _dataLogs.Add(receiveLogDataObject);
            }
        }

        void StartStopLogging(LogCommands command)
        {
            var channels = _activeSubscriptions.Select(s => s.ChannelNo).ToList();
            _logDataStreamManager.SendStartStopLoggingRequest(command, channels);
        }

    }

    public interface ILogManager
    {
        //void StartStopLogging(LogCommands command);
    }
}
