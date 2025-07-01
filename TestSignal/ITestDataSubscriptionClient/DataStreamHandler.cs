using MotionMonitor.Enums;
using System.Net.Sockets;
using TestSignal;

namespace MotionMonitor
{
    public class DataStreamHandler
    {
        private const int COMMAND_TIMEOUT = 500;
        private ManualResetEvent _commandExecuted;
        public DataStreamHandler()
        {
            _commandExecuted = new ManualResetEvent(false);
        }
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


        public void OnAllSubscriptionsRemoved(int status)
        {
            lock (_definedSignals)
            {
                _definedSignals.Clear();
                _orderedSignals.Clear();
            }
            lock (_trigs)
            {
                _trigs.Clear();
            }
            _commandExecuted.Set();
        }
    }
}
