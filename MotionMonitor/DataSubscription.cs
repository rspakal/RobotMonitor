namespace MotionMonitor
{
    public class DataSubscription
    {
        private const int VELOCITY_SIGNAL = 1717;
        private const int TORQUE_SIGNAL = 4947;
        private const string ROBOT_NAME = "ROB_1";

        private int _signalNo;
        private string _mechUnitName;
        private int _axisNo;
        private Trig _trig;
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
        public Trig Trig
        {
            get => _trig;
            set => _trig = value;
        }
        public DataSubscription(string mechUnitName, int axisNo, int signalNo, Trig trig)
        {
            _mechUnitName = mechUnitName;
            _axisNo = axisNo;
            _signalNo = signalNo;
            _trig = trig;
        }

        public static DataSubscription[] BuildDataSubscribtions()
        {
            DataSubscription[] subscriptions = new DataSubscription[12];
            int signalNo;
            for (int i = 0; i < 12; i++)
            {
                signalNo = i < 6 ? VELOCITY_SIGNAL : TORQUE_SIGNAL;
                subscriptions[i] = new DataSubscription(ROBOT_NAME, i + 1, signal);
            }
            return signals;
        }

    }


}
