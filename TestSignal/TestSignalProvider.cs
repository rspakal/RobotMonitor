using TestSignalLogger;
namespace TestSignal
{
	public class TestSignalProvider : IMeasurementsProvider, IDisposable, ITestSignalSubscriber
	{
		private const int StoMS = 1000;

		private readonly int signalNumber;

		private readonly int axisNo;

		private readonly double sampleTime;

		private readonly string mechUnitName;

		private readonly bool antiAliasFiltering;

		private readonly Trig trig;

		public bool RequiresCommonHandler
		{
			get
			{
				return true;
			}
		}

		public string HandlerKey
		{
			get
			{
				return "TestSignalHandler";
			}
		}

		private TestSignalProvider()
		{
		}

		public TestSignalProvider(string mechUnitName, int signalNumber, int axisNo, double sampleTime, bool antiAliasFiltering, Trig trig)
		{
			this.mechUnitName = mechUnitName;
			this.signalNumber = signalNumber;
			this.axisNo = axisNo;
			this.sampleTime = sampleTime;
			this.antiAliasFiltering = antiAliasFiltering;
			if (this.antiAliasFiltering)
			{
				this.sampleTime = Signal.AxcSampleTime * 1000.0;
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
			return new TestSignalSubscriptionItem[1]
			{
			new TestSignalSubscriptionItem(signalNumber, mechUnitName, axisNo, trig)
			};
		}

		public double GetSampleTime()
		{
			return sampleTime;
		}
	}
}
