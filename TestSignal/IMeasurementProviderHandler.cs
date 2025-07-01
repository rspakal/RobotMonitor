using TestSignal;

namespace MotionMonitor.Enums
{
	public interface IMeasurementProviderHandler
	{
		bool Init(IMeasurementsProvider[] providers);
		void Start();
		void Pause();
		void Stop();
	}
}
