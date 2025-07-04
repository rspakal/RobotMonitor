using Microsoft.VisualBasic;
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

        private const int COMMAND_TIMEOUT = 500;
        private ManualResetEvent _commandExecuted;
        private DataSubscriptionClient _subscriptionClient;
        public DataStreamHandler(DataSubscriptionClient subscriptionClient)
        {
            _subscriptionClient = subscriptionClient;
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
        public void RemoveAllSubscribtions(LogSrvCommand cmd, ManualResetEvent commandExecuted)
        {
            _commandExecuted.Reset();
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData((int)cmd);
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


        public void OnAllSubscriptionsRemoved(int status)
        {
            _subscriptionClient.RemoveAllActiveSubscriptions(_commandExecuted);
        }

        protected override void OnSubscriptionActivated(int channel, LogSrvSignalDefinition signal, DataSubscription dataSubscription)
        {
            _subscriptionClient.AddActiveSubscription(dataSubscription, _commandExecuted);
        }
    }
}
