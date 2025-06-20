namespace TestSignal
{
	public interface IMeasurementProviderHandler
	{
		bool Init(IMeasurementsProvider[] providers);
		void Start();
		void Pause();
		void Stop();
	}
}
