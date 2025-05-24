using TestSignalLogger;
namespace TestSignal
{
    public class TestSignalSubscriptionItem
    {
        public int SignalNumber;

        public string MechUnitName;

        public int AxisNumber;

        public Trig Trig;

        public TestSignalSubscriptionItem(int signalNumber, string mechUnitName, int axisNumber, Trig trig)
        {
            SignalNumber = signalNumber;
            MechUnitName = mechUnitName;
            AxisNumber = axisNumber;
            Trig = trig;
        }
    }
}
