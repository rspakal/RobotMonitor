namespace TestSignal
{
	public interface ITestSignalSubscriber
	{
		TestSignalSubscriptionItem[] GetSubscriptionItems();

		double GetSampleTime();
	}
}
