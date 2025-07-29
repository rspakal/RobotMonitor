namespace MotionMonitor
{
    public class RequestDataSubscription : DataSubscription
    {

        public RequestDataSubscription(int channelNo, int signalNo, string mechUnitName, int axisNo, float sampleTime) :
            base(signalNo, mechUnitName, axisNo, sampleTime)
        {
        }

        public static Dictionary<int, RequestDataSubscription> BuildRequestedDataSubscribtions()
        {
            Dictionary<int, RequestDataSubscription> subscriptions = [];
            int signalNo;
            int axisNo;
            for (int i = 0; i < 12; i++)
            {
                if (i < 6)
                {
                    axisNo = i + 1;
                    signalNo = VELOCITY_SIGNAL;
                }
                else
                {
                    axisNo = i - 5;
                    signalNo = TORQUE_SIGNAL;
                }

                subscriptions.Add(i, new RequestDataSubscription(i, signalNo, ROBOT_NAME, axisNo, AXC_SAMPLE_TIME));
            }
            return subscriptions;
        }

    }


}
