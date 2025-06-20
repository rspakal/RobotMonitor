namespace TestSignal
{

	public interface ITestDataSubscriptionClient
	{
		bool Connected { get; }
		bool Subscribing { get; }
		event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
		event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;
		void Connect();
		void AddSubscriber(IDataSubscriber subscriber);
		void StartSubscription();
		void StopSubscription();
		void Disconnect();
	}
}
