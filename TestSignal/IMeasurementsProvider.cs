namespace TestSignal
{
	public interface IMeasurementsProvider : IDisposable
	{
		bool RequiresCommonHandler { get; }
		string HandlerKey { get; }
		void Start();
		void Stop();
	}
}
