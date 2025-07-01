namespace TestSignal
{
	public interface IDataSubscriber
	{
		SubscriptionData GetSubscriptionData();
		double SampleTime { get; }
	}
}
