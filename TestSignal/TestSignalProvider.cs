using TestSignalLogger;
namespace TestSignal
{
    public class TestSignalProvider : IMeasurementsProvider, IDisposable, ITestSignalSubscriber
    {
        private const int StoMS = 1000;
        private readonly int _signalNumber;
        private readonly int _axisNo;
        private readonly double _sampleTime;
        private readonly string mechUnitName;
        private readonly bool _antiAliasFiltering;
        private readonly Trig trig;

        public double SampleTime => _sampleTime;

        public bool RequiresCommonHandler => true;
        public string HandlerKey => "TestSignalHandler";
        private TestSignalProvider()
        {
        }

        public TestSignalProvider(string mechUnitName, int signalNumber, int axisNo, double sampleTime, bool antiAliasFiltering, Trig trig)
        {
            this.mechUnitName = mechUnitName;
            _signalNumber = signalNumber;
            _axisNo = axisNo;
            _sampleTime = sampleTime;
            _antiAliasFiltering = antiAliasFiltering;
            if (_antiAliasFiltering)
            {
                _sampleTime = Signal.AxcSampleTime * 1000.0;
            }
            this.trig = trig;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }

        public TestSignalSubscriptionItem[] GetSubscriptionItems()
        {
            return [new TestSignalSubscriptionItem(_signalNumber, mechUnitName, _axisNo, trig)];
        }
    }
}
