namespace MotionMonitor
{
    internal class MainClass
    {
        private ConnectionClient? _connectionClient;
        private DataExchangeClient? _dataExchangeClient;
        private DataSubscriptionClient? _subscriptionClient;
        private DataLogClient? _dataLogClient;

        private async Task<bool> Init()
        {
            _connectionClient ??= new ConnectionClient();
            var socket = await _connectionClient.InitAsync();
            if (socket is null)
            {
                return false;
            }

            _dataExchangeClient ??= new DataExchangeClient();
            if (!_dataExchangeClient.Init(socket))
            {
                return false;
            }

            _subscriptionClient ??= new DataSubscriptionClient(_dataExchangeClient);
            if (!_subscriptionClient.Init())
            {
                return false;
            }

            _dataLogClient ??= new DataLogClient(_dataExchangeClient, _subscriptionClient.ActiveSubscriptions);
            if (!_dataLogClient.Init())
            {
                return false;
            }

            return true;

        }
    }
}
 