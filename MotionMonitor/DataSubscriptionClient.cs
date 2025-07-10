namespace MotionMonitor
{
    public class DataSubscriptionClient : ISubscriptionManager
    {
        const int COMMAND_TIMEOUT = 500;
        const int MAX_SUBSCRIPTIONS_AMOUNT = 12;

        private List<RequestDataSubscription> _requestedDataSubscriptions;
        private List<ActiveDataSubscription> _activeSubscriptions;
        private int _lastChannelNo;
        private ISubscriptionDataStreamManager _subscriptionDataStreamManager;

        public List<ActiveDataSubscription> ActiveSubscriptions => _activeSubscriptions;

        event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
        event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;

        public DataSubscriptionClient(ISubscriptionDataStreamManager subscriptiondataStreamManager)
        {
            _subscriptionDataStreamManager = subscriptiondataStreamManager;
            _subscriptionDataStreamManager.SubscriptionManager = this;
            _requestedDataSubscriptions = RequestDataSubscription.BuildRequestedDataSubscribtions();
            _activeSubscriptions = new();
        }

        public bool Init()
        {
            if (_requestedDataSubscriptions is null)
            {
                _requestedDataSubscriptions = RequestDataSubscription.BuildRequestedDataSubscribtions();
            }
            else
            {
                _requestedDataSubscriptions.Clear();
                _requestedDataSubscriptions.AddRange(RequestDataSubscription.BuildRequestedDataSubscribtions());
            }

            while (CancelAllSubscriptions())
            {
                Thread.Sleep(COMMAND_TIMEOUT);
            }

            foreach (var subscription in _requestedDataSubscriptions)
            {
                if (!Subscribe(subscription))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Subscribe(RequestDataSubscription dataSubscription)
        {
            if (_activeDataSubscriptions.Count >= MAX_SUBSCRIPTIONS_AMOUNT)
            {
                throw new Exception(string.Format($"Cannot add more channels, max is {MAX_SUBSCRIPTIONS_AMOUNT}."));
            }

            channel = FindChannel(dataSubscription);
            if (!IsSubscriptionActive(dataSubscription))
            {
                try
                {
                    _subscriptionManager.SendSubscriptionRequest(dataSubscription);
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return true;
        }

        private void CancelSubscription()
        { }

        private bool CancelAllSubscriptions()
        {
            try
            {
                _subscriptionManager.SendCancelAllSubscribtionsRequest();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        #region ISubscriptionManager methods
        void ISubscriptionManager.AddActiveSubscription(ActiveDataSubscription dataSubscription)
        {
            lock (_activeDataSubscriptions)
            {
                _activeDataSubscriptions.Add(dataSubscription);
                _lastChannelNo = dataSubscription.ChannelNo;
            }
        }

        void ISubscriptionManager.RemoveActiveSubscription(ActiveDataSubscription activeDataSubscription)
        {
            throw new NotImplementedException();
        }

        void ISubscriptionManager.RemoveAllActiveSubscriptions()
        {
            lock (_activeDataSubscriptions)
            {
                _activeDataSubscriptions.Clear();
            }

            lock (_trigs)
            {
                _trigs.Clear();
            }

        }

        void ISubscriptionManager.EnumerateActiveSubscriptions(List<ActiveDataSubscription> activeDataSubscriptions)
        {
            lock (_activeDataSubscriptions)
            {
                _activeDataSubscriptions.Clear();
                foreach (var subscription in activeDataSubscriptions)
                {
                    if (subscription.Enabled)
                    {
                        _activeDataSubscriptions.Add(subscription);
                    }
                }
            }
        }

        ActiveDataSubscription? ISubscriptionManager.GetActiveSubscription(int channelNo)
        {
            var activeSubscription = _activeDataSubscriptions.FirstOrDefault(s => s.ChannelNo == channelNo);
            return activeSubscription;
        }
        
        public double GetActiveSubscriptionsMinSampleTime()
        {
            return _activeDataSubscriptions?.Select(v => v.SampleTime).DefaultIfEmpty(-1).Min() ?? -1;
        }

        public bool IsActiveDataSubscribtionExist()
        {
            return true;
        }
        #endregion

        private bool IsSubscriptionActive(RequestDataSubscription subscription)
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
    public interface ISubscriptionsManager
    {
        public void AddActiveSubscription(ActiveDataSubscription activeDataSubscription);
        public void RemoveActiveSubscription(ActiveDataSubscription activeDataSubscription);
        public void RemoveAllActiveSubscriptions();
        public void EnumerateActiveSubscriptions(List<ActiveDataSubscription> activeDataSubscription);

        public ActiveDataSubscription? GetActiveSubscription(int channelNo);
        public double GetActiveSubscriptionsMinSampleTime();
        public bool IsActiveDataSubscribtionExist();
    }
}
