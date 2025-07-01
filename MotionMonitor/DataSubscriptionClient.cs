using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class DataSubscriptionClient
    {
        const int COMMAND_TIMEOUT = 500;

        private List<DataSubscription> _dataSubscriptions;
        private ManualResetEvent _commandExecuted;
        private ConnectionClient _connectionHandler;

        private DataStreamHandler _streamHandler;
        public DataSubscriptionClient()
        {
            _streamHandler = new DataStreamHandler();
            _dataSubscriptions = DataSubscription.BuildDataSubscriptions();
            _commandExecuted = new ManualResetEvent(false);
        }
        bool Connected { get; }
        bool Subscribing { get; }
        event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
        event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;
        public async Task ConnectAsync()
        {
            try
            {
                await _connectionHandler.ConnectAsync();
                RemoveAllSubscriptions();
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Error, "TestSignalHandler::Connect()", ex);
                await _connectionHandler.DisconnectAsync();
                throw;
            }
        }
        public void AddSubscriber(IDataSubscriber subscriber)

        { }
        public void StartSubscription()
        { }
        public void StopSubscription()
        { }
        public void Disconnect()
        { }


        private void RemoveAllSubscriptions()
        {
            _commandExecuted.Reset();
            _streamHandler.RemoveAllSubscribtions(LogSrvCommand.RemoveAllSignals);
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                throw new TimeoutException("RemoveAllSignals");
            }
        }

        public void AddSubscription(string mechUnitName, int axisNumber, int signalNumber, double sampleTime, out int channel)
        {
            if (!CanAddChannel())
            {
                throw new Exception(string.Format("Cannot add more channels, max is {0}.", base.MAX));
            }

            channel = FindChannel(signalNumber, mechUnitName, axisNumber, sampleTime);
            if (!SignalDefined(signalNumber, mechUnitName, axisNumber, sampleTime))
            {
                _orderedSignals.Add(new Signal(mechUnitName, axisNumber, signalNumber));
                _commandExecuted.Reset();
                WriteDefineSignal(channel, signalNumber, mechUnitName, axisNumber, (float)sampleTime);
                if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
                {
                    throw new TimeoutException("DefineSignal");
                }
            }
        }
    }
}
