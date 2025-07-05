using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public interface IDataSubscriptionsHandler
    {
        public void AddActiveSubscription(ActiveDataSubscription activeDataSubscription);
        public void RemoveActiveSubscription(ActiveDataSubscription activeDataSubscription);
        public void RemoveAllActiveSubscriptions();
        public void EnumerateActiveSubscriptions(List<ActiveDataSubscription> activeDataSubscription);
        public ActiveDataSubscription? GetActiveSubscription(int channelNo);
        public bool IsActiveDataSubscribtionExist();
    }
}
