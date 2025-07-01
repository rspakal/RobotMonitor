using MotionMonitor;
using MotionMonitor.Enums;

namespace TestSignal
{

	public class DataSubscriptionClient
	{
        const int COMMAND_TIMEOUT = 500;
        private ManualResetEvent _commandExecuted;

        private DataStreamHandler _streamHandler;
        public DataSubscriptionClient()
        {
            _streamHandler = new DataStreamHandler();
            _commandExecuted = new ManualResetEvent(false);
        }
        bool Connected { get; }
		bool Subscribing { get; }
		event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;
		event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;
        void Connect()
        {
            try
            {
                Connect();
                RemoveAllSubscriptions();
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Error, "TestSignalHandler::Connect()", ex);
                Disconnect();
                throw;
            }
        }
        public void AddSubscriber(IDataSubscriber subscriber)
		{ }
		public void StartSubscription()
		{ }
		public void StopSubscription()
		{ }
		public void Disconnect()
		{ }


        private void RemoveAllSubscriptions()
        {
            _commandExecuted.Reset();
            _streamHandler.RemoveAllSubscribtions(LogSrvCommand.RemoveAllSignals);
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                throw new TimeoutException("RemoveAllSignals");
            }
        }
    }
}
