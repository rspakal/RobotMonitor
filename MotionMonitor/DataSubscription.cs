namespace MotionMonitor
{
    public abstract class DataSubscription
    {
        public const double SAMPLE_TIME = 0.000504;

        protected const int VELOCITY_SIGNAL = 1717;
        protected const int TORQUE_SIGNAL = 4947;
        protected const string ROBOT_NAME = "ROB_1";
        protected const float AXC_SAMPLE_TIME = 0.000504f;
        public const int MECH_UNIT_NAME_LENGTH = 40;

        //protected int _channelNo;
        protected int _signalNo;
        protected string _mechUnitName;
        protected int _axisNo;
        protected float _sampleTime;


        //public int ChannelNo
        //{
        //    get => _channelNo;
        //    set => _channelNo = value;
        //}
        public int SignalNo
        {
            get => _signalNo;
            set => _signalNo = value;
        }
        public string MechUnitName
        {
            get => _mechUnitName;
            set => _mechUnitName = value;
        }
        public int AxisNo
        {
            get => _axisNo;
            set => _axisNo = value;
        }
        public float SampleTime
        {
            get => _sampleTime;
            set => _sampleTime = value;
        }

        public DataSubscription(int signalNo, string mechUnitName, int axisNo, float sampleTime)
        {
            _signalNo = signalNo;
            _mechUnitName = mechUnitName;
            _axisNo = axisNo;
            _sampleTime = sampleTime;
        }

        //public static List<DataSubscription> BuildRequestedDataSubscribtions()
        //{
        //    List<DataSubscription> subscriptions = new();
        //    int signalNo;
        //    int axisNo = 1;
        //    for (int i = 0; i < 12; i++)
        //    {
        //        signalNo = i <= 6 ? VELOCITY_SIGNAL : TORQUE_SIGNAL;
        //        if (i < 6)
        //        {
        //            axisNo = i + 1;
        //            signalNo = VELOCITY_SIGNAL;
        //        }
        //        else
        //        {
        //            axisNo = i - 5;
        //            signalNo = TORQUE_SIGNAL;
        //        }

        //        subscriptions.Add(new DataSubscription(i, signalNo, ROBOT_NAME, axisNo, AXC_SAMPLE_TIME));
        //    }
        //    return subscriptions;
        //}

    }


}
