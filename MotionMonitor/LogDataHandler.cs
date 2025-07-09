using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    internal class LogDataHandler
    {
        List<>
        private void PrepareLogDataBuffer(List<ActiveDataSubscription> activeDataSubscriptions)
        {
            foreach (var dataSubscription in activeDataSubscriptions)
            {
                ReceiveLogDataObject receiveLogDataObject = new();
                if (AntiAliasFiltering)
                {
                    double sampleTime = dataSubscription.SampleTime;
                    int num = (int)Math.Round(SampleTime / sampleTime, 0);
                    Filter antiAliasFilter = null;
                    if (num > 1)
                    {
                        antiAliasFilter = new Cheby1Filter(num);
                    }
                    receiveLogDataObject.AntiAliasFilter = antiAliasFilter;
                }
                receiveLogDataObject.LoggedData = new();
                receiveLogDataObject.LoggedSamples = 0;
                receiveLogDataObject.ReceivedLogData = 0;
                _dataLogs.Add(key, receiveLogDataObject);
            }
        }

    }
}
