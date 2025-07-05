namespace MotionMonitor
{
    public class RequestDataSubscription : DataSubscription
    {

        public RequestDataSubscription(int channelNo, int signalNo, string mechUnitName, int axisNo, float sampleTime) :
            base(channelNo, signalNo, mechUnitName, axisNo, sampleTime)
        {
        }

        public static List<RequestDataSubscription> BuildRequestedDataSubscribtions()
        {
            List<RequestDataSubscription> subscriptions = new();
            int signalNo;
            int axisNo = 1;
            for (int i = 0; i < 12; i++)
            {
                signalNo = i <= 6 ? VELOCITY_SIGNAL : TORQUE_SIGNAL;
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

                subscriptions.Add(new RequestDataSubscription(i, signalNo, ROBOT_NAME, axisNo, AXC_SAMPLE_TIME));
            }
            return subscriptions;
        }

    }


}
