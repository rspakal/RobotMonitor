using TestSignalLogger;
namespace TestSignal
{
    public class DataProvider : IMeasurementsProvider, IDataSubscriber, IDisposable
    {
        private readonly int _signalNo;
        private readonly string _mechUnitName;
        private readonly int _axisNo;
        private readonly Trig _trig;

        private readonly SubscriptionData _subscription;
        private readonly double _sampleTime;
        private readonly bool _antiAliasFiltering;

        public double SampleTime => _sampleTime;
        public bool RequiresCommonHandler => true;
        public string HandlerKey => "TestSignalHandler";
        public DataProvider(string mechUnitName, int signalNo, int axisNo, double sampleTime, bool antiAliasFiltering, Trig trig)
        {
            _mechUnitName = mechUnitName;
            _signalNo = signalNo;
            _axisNo = axisNo;
            _sampleTime = sampleTime;
            _antiAliasFiltering = antiAliasFiltering;
            if (_antiAliasFiltering)
            {
                _sampleTime = Signal.AxcSampleTime * 1000.0;
            }
            _trig = trig;
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

        public SubscriptionData GetSubscriptionData()
        {
            return new SubscriptionData(_mechUnitName, _axisNo, _signalNo, _trig);
        }
    }
}
