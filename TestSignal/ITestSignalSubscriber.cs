namespace TestSignal
{
	public interface IDataSubscriber
	{
		SubscriptionData[] GetSubscriptionItems();
		double SampleTime { get; }
	}
}
