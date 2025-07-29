namespace MotionMonitor
{
    public class DataSubscriptionClient : ISubscriptionManager
    {
        const int COMMAND_TIMEOUT = 500;
        const int MAX_SUBSCRIPTIONS_AMOUNT = 12;

        private Dictionary<int, RequestDataSubscription> _requestedSubscriptions;
        private Dictionary<int, ActiveDataSubscription> _activeSubscriptions;
        private ISubscriptionDataStreamManager _subscriptionDataStreamManager;
        private int _lastChannelNo;

        public Dictionary<int, ActiveDataSubscription> ActiveSubscriptions => _activeSubscriptions;
        //event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
        //event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;

        public DataSubscriptionClient(ISubscriptionDataStreamManager subscriptiondataStreamManager)
        {
            _subscriptionDataStreamManager = subscriptiondataStreamManager;
            _subscriptionDataStreamManager.SubscriptionManager = this;
            _requestedSubscriptions = RequestDataSubscription.BuildRequestedDataSubscribtions();
            _activeSubscriptions = [];
        }

        public bool Init()
        {
            if (_requestedSubscriptions is null)
            {
                _requestedSubscriptions = RequestDataSubscription.BuildRequestedDataSubscribtions();
            }
            else
            {
                _requestedSubscriptions.Clear();
                foreach (var s in RequestDataSubscription.BuildRequestedDataSubscribtions())
                {
                    _requestedSubscriptions.Add(s.Key, s.Value);
                }
            }

            if (!CancelAllSubscriptions())
            {
                return false;
            }

            foreach (var subscription in _requestedSubscriptions)
            {
                if (!Subscribe(subscription))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Subscribe(KeyValuePair<int, RequestDataSubscription> dataSubscription)
        {
            if (_activeSubscriptions.Count >= MAX_SUBSCRIPTIONS_AMOUNT)
            {
                throw new Exception(string.Format($"Cannot add more channels, max is {MAX_SUBSCRIPTIONS_AMOUNT}."));
            }

            var channel = dataSubscription.Key;
            if (!IsSubscriptionActive(dataSubscription.Value))
            {
                try
                {
                    _subscriptionDataStreamManager.SendSubscriptionRequest(dataSubscription);
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
                _subscriptionDataStreamManager.SendCancelAllSubscribtionsRequest();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #region ISubscriptionManager methods
        void ISubscriptionManager.AddActiveSubscription(int channelNo, ActiveDataSubscription dataSubscription)
        {
            lock (_activeSubscriptions)
            {
                _activeSubscriptions.Add(channelNo, dataSubscription);
                _lastChannelNo = channelNo;
            }
        }

        void ISubscriptionManager.RemoveActiveSubscription(int channel, ActiveDataSubscription activeDataSubscription)
        {
            throw new NotImplementedException();
        }

        void ISubscriptionManager.RemoveAllActiveSubscriptions()
        {
            lock (_activeSubscriptions)
            {
                _activeSubscriptions.Clear();
            }

            //lock (_trigs)
            //{
            //    _trigs.Clear();
            //}

        }

        void ISubscriptionManager.EnumerateActiveSubscriptions(Dictionary<int, ActiveDataSubscription> activeDataSubscriptions)
        {
            lock (_activeSubscriptions)
            {
                _activeSubscriptions.Clear();
                foreach (var subscription in activeDataSubscriptions)
                {
                    if (subscription.Value.Enabled)
                    {
                        _activeSubscriptions.Add(subscription.Key, subscription.Value);
                    }
                }
            }
        }

        ActiveDataSubscription? ISubscriptionManager.GetActiveSubscription(int channelNo)
        {
            var activeSubscription = _activeSubscriptions[channelNo];
            return activeSubscription;
        }
        
        public double GetActiveSubscriptionsMinSampleTime()
        {
            return _activeSubscriptions?.Select(v => v.Value.SampleTime).DefaultIfEmpty(-1).Min() ?? -1;
        }

        public bool IsActiveDataSubscribtionExist()
        {
            return true;
        }
        #endregion

        private bool IsSubscriptionActive(RequestDataSubscription subscription)
        {
            var result = _activeSubscriptions.Any(s =>
            s.Value.AxisNo == subscription.AxisNo &&
            s.Value.MechUnitName == subscription.MechUnitName &&
            s.Value.SignalNo == subscription.SignalNo &&
            s.Value.SampleTime == subscription.SampleTime);

            return result;
        }
        private int FindChannel(DataSubscription dataSubscription)
        {
            var channel = _activeSubscriptions.FirstOrDefault(s =>
            s.Value.SignalNo == dataSubscription.SignalNo &&
            s.Value.MechUnitName == dataSubscription.MechUnitName &&
            s.Value.AxisNo == dataSubscription.AxisNo &&
            s.Value.SampleTime == dataSubscription.SampleTime);

            int num = MAX_SUBSCRIPTIONS_AMOUNT - 1;
            foreach (var activeDataSubscription in _activeSubscriptions)
            {
                if (activeDataSubscription.Value.SignalNo == dataSubscription.SignalNo &&
                    activeDataSubscription.Value.MechUnitName == dataSubscription.MechUnitName &&
                    activeDataSubscription.Value.AxisNo == dataSubscription.AxisNo &&
                    activeDataSubscription.Value.SampleTime == dataSubscription.SampleTime)
                {
                    return num;
                }
                num--;
            }
            return num;
        }

    }
    public interface ISubscriptionManager
    {
        public void AddActiveSubscription(int channelNo, ActiveDataSubscription activeDataSubscription);
        public void RemoveActiveSubscription(int channelNo, ActiveDataSubscription activeDataSubscription);
        public void RemoveAllActiveSubscriptions();
        public void EnumerateActiveSubscriptions(Dictionary<int, ActiveDataSubscription> activeDataSubscription);

        public ActiveDataSubscription? GetActiveSubscription(int channelNo);
        public double GetActiveSubscriptionsMinSampleTime();
        public bool IsActiveDataSubscribtionExist();
    }
}
