﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public interface ISubscriptionManager
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
