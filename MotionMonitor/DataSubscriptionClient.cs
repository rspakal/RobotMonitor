using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class DataSubscriptionClient
    {
        const int COMMAND_TIMEOUT = 500;
        const int MAX_SUBSCRIPTIONS_AMOUNT = 12;

        private List<DataSubscription> _requestedDataSubscriptions;
        private List<DataSubscription> _activeDataSubscriptions;
        //private ManualResetEvent _commandExecuted;
        private readonly ConnectionClient _connectionHandler;
        private readonly DataStreamHandler _dataStreamHandler;
        public DataSubscriptionClient()
        {
            _connectionHandler = new ConnectionClient();
            _dataStreamHandler = new DataStreamHandler(this);
            _requestedDataSubscriptions = DataSubscription.BuildRequestedDataSubscribtions();
            _activeDataSubscriptions = new();
            //_commandExecuted = new ManualResetEvent(false);
        }

        public bool Init()
        {
            bool isSucceed = true;
            try
            {
                Connect();
                _requestedDataSubscriptions ??= DataSubscription.BuildRequestedDataSubscribtions();
                for (int i = 0; i < _requestedDataSubscriptions.Count; i++)
                {
                    Subscribe(_requestedDataSubscriptions[i]);
                }
            }
            catch (Exception)
            {
                isSucceed =false;
            }

            return isSucceed;
        }

        bool Connected { get; }
        bool Subscribing { get; }
        event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
        event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;
        public void Connect()
        {
            try
            {
                _connectionHandler.ConnectAsync().Wait();
                RemoveAllSubscriptions();
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Error, "TestSignalHandler::Connect()", ex);
                _connectionHandler.DisconnectAsync().Wait();
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
            _dataStreamHandler.RemoveAllSubscribtions(LogSrvCommand.RemoveAllSignals);
        }

        private void Subscribe(DataSubscription dataSubscription)
        {
            if (_activeDataSubscriptions.Count >= MAX_SUBSCRIPTIONS_AMOUNT)
            {
                throw new Exception(string.Format($"Cannot add more channels, max is {MAX_SUBSCRIPTIONS_AMOUNT}."));
            }

            channel = FindChannel(dataSubscription);
            if (!IsSignalDefined(dataSubscription))
            {
                _dataStreamHandler.SendSubscriptionRequest(dataSubscription);
            }
        }

        public void AddActiveSubscription(DataSubscription dataSubscription, ManualResetEvent commandExecuted)
        {
            lock (_activeDataSubscriptions)
            {
                _activeDataSubscriptions.Add(dataSubscription);
                _lastChannel = channel;
            }
            commandExecuted.Set();
        }

        public void RemoveAllActiveSubscriptions(ManualResetEvent commandExecuted)
        {
            lock (_activeDataSubscriptions)
            {
                _activeDataSubscriptions.Clear();
            }

            lock (_trigs)
            {
                _trigs.Clear();
            }

            commandExecuted.Set();
        }

        private bool IsSignalDefined(DataSubscription subscription)
        {
            var result = _activeDataSubscriptions.Any(s =>
            s.AxisNo == subscription.AxisNo &&
            s.MechUnitName == subscription.MechUnitName &&
            s.SignalNo == subscription.SignalNo &&
            s.SampleTime == subscription.SampleTime);

            return result;
        }
        private int FindChannel(DataSubscription dataSubscription)
        {
            var channel = _activeDataSubscriptions.FirstOrDefault(s => 
            s.SignalNo == dataSubscription.SignalNo && 
            s.MechUnitName == dataSubscription.MechUnitName && 
            s.AxisNo == dataSubscription.AxisNo && 
            s.SampleTime == dataSubscription.SampleTime);

            int num = MAX_SUBSCRIPTIONS_AMOUNT - 1;
            foreach (var activeDataSubscription in _activeDataSubscriptions)
            {
                if (activeDataSubscription.SignalNo == dataSubscription.SignalNo &&
                    activeDataSubscription.MechUnitName == dataSubscription.MechUnitName &&
                    activeDataSubscription.AxisNo == dataSubscription.AxisNo &&
                    activeDataSubscription.SampleTime == dataSubscription.SampleTime)
                {
                    return num;
                }
                num--;
            }
            return num;
        }
    }
}
