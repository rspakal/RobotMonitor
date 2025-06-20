using TestSignalLogger;
namespace TestSignal
{
    public class TestSignalProvider : IMeasurementsProvider, IDisposable, IDataSubscriber
    {
        private const int StoMS = 1000;
        private readonly int _signalNumber;
        private readonly int _axisNo;
        private readonly double _sampleTime;
        private readonly string _mechUnitName;
        private readonly bool _antiAliasFiltering;
        private readonly Trig trig;

        public double SampleTime => _sampleTime;
        public bool RequiresCommonHandler => true;
        public string HandlerKey => "TestSignalHandler";
        public TestSignalProvider(string mechUnitName, int signalNumber, int axisNo, double sampleTime, bool antiAliasFiltering, Trig trig)
        {
            _mechUnitName = mechUnitName;
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

        public SubscriptionData[] GetSubscriptionItems()
        {
            return [new SubscriptionData(_signalNumber, _mechUnitName, _axisNo, trig)];
        }
    }
}
