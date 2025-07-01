using TestSignalLogger;
namespace TestSignal
{
    public class SubscriptionData
    {
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
        public SubscriptionData(string mechUnitName, int axisNo, int signalNo, Trig trig)
        {
            _mechUnitName = mechUnitName;
            _axisNo = axisNo;
            _signalNo = signalNo;
            _trig = trig;
        }
    }
}
