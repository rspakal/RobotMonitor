namespace MotionMonitor
{
    internal class MainClass
    {
        private readonly byte[] _ipAddressBytes = { 192, 168, 125, 1 };
        private const int PORT = 4011;

        private ConnectionClient? _connectionClient;
        private DataExchangeClient? _dataExchangeClient;
        private DataSubscriptionClient? _subscriptionClient;
        private LogDataClient? _dataLogClient;

        private async Task<bool> Iniit()
        {
            _connectionClient ??= new ConnectionClient(_ipAddressBytes, PORT);
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

            _dataLogClient ??= new LogDataClient();
            if (_dataLogClient.Init())
            {
                return false;
            }

            return true;

        }
    }
}
 