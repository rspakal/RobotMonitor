using MotionMonitor.Enums;
using System.Net;
using System.Net.Sockets;
using TestSignalLogger;
namespace TestSignal
{
    public class TestSignalHandler : LogSrvStreamHandler, IDisposable, ITestDataSubscriptionClient, IMeasurementProviderHandler
    {
        private const int LOGSRVPORT = 4011;
        private readonly TimeSpan CONNECTION_TIMEOUT = new TimeSpan(0, 0, 6);
        private const int COMMAND_TIMEOUT = 500;
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private readonly IPAddress _ipAddress;
        private Socket _socket;
        private ManualResetEvent _connectionComplete;
        private ManualResetEvent _commandExecuted;
        private ConnectionState _connectionState;
        private Dictionary<int, LogSrvSignalDefinition> _definedSignals;
        private Dictionary<int, ReceiveLogDataObject>? _dataLogs;
        private List<Signal> _orderedSignals;
        private int _lastChannel;
        private Dictionary<int, Trig> _trigs;
        private bool _subscribing;
        private bool _antiAliasFiltering;

        public IPAddress IPAddress => _ipAddress;
        public ConnectionState ConnectionState => _connectionState;
        public bool AntiAliasFiltering
        {
            get => _antiAliasFiltering && Math.Abs(GetMinSampleTime() - Signal.AxcSampleTime * 1000.0) < 1.0;
            set => _antiAliasFiltering = value;
        }
        public double SampleTime { get; set; }
        public bool ZeroOrderHold { get; set; }
        public int MaxLogTime { get; set; }
        public TrigHandler TrigHandler { get; set; }
        bool ITestDataSubscriptionClient.Connected => ConnectionState == ConnectionState.Connected;
        bool ITestDataSubscriptionClient.Subscribing => _subscribing;

        public event EventHandler<PropertyChangedEventArgs<ConnectionState>> ConnectionStateChanged;

        public event EventHandler<NotifyEventArgs<string>> Notification;

        private event EventHandler<NotifyEventArgs<bool>> ConnectedChanged;

        private event EventHandler<NotifyEventArgs<bool>> SubscribingChanged;

        public event EventHandler TestSignalRecived;

        event EventHandler<NotifyEventArgs<bool>> ITestDataSubscriptionClient.ConnectedChanged
        {
            add
            {
                ConnectedChanged += value;
            }
            remove
            {
                ConnectedChanged -= value;
            }
        }

        event EventHandler<NotifyEventArgs<bool>> ITestDataSubscriptionClient.SubscribingChanged
        {
            add
            {
                SubscribingChanged += value;
            }
            remove
            {
                SubscribingChanged -= value;
            }
        }

        public TestSignalHandler(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
            TrigHandler = new TrigHandler();
        }

        private void Init()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveBufferSize = RECEIVE_BUFFER_SIZE
            };
            _connectionComplete = new ManualResetEvent(false);
            _commandExecuted = new ManualResetEvent(false);
            _connectionState = ConnectionState.Idle;
            _definedSignals = new Dictionary<int, LogSrvSignalDefinition>();
            _orderedSignals = new List<Signal>();
            if (_dataLogs != null && _dataLogs.Count > 0)
            {
                foreach (var value in _dataLogs.Values)
                {
                    value.AntiAliasFilter = null;
                    value.LoggedData = null;
                }
                _dataLogs = null;
                GC.Collect();
            }
            _dataLogs = new Dictionary<int, ReceiveLogDataObject>();
            _trigs = new Dictionary<int, Trig>();
            _subscribing = false;
        }

        private void Connect()
        {
            Init();
            SetConnectionState(ConnectionState.Connecting);
            try
            {
                _connectionComplete.Reset();
                _socket.BeginConnect(_ipAddress, LOGSRVPORT, ConnectCallback, _socket);
                _connectionComplete.WaitOne(CONNECTION_TIMEOUT, true);
                SetConnectionState(ConnectionState.Connected);
                StartStreamHandler(_socket, false);
            }
            catch
            {
                NotifyMessage("Connection failed.");
                SetConnectionState(ConnectionState.Idle);
            }
        }

        private void Disconnect()
        {
            //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", "Disconnecting!");
            if (!ValidateConnectionState(ConnectionState.Connected))
            {
                return;
            }
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(false);
            }
            catch (Exception ex)
            {
                NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex);
            }
            try
            {
                StopStreamHandler();
                SetConnectionState(ConnectionState.Disconnecting);
                _socket.Close();
            }
            catch (Exception ex2)
            {
                NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex2);
            }
            try
            {
                SetConnectionState(ConnectionState.Idle);
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", "Disconnect complete!");
            }
            catch (Exception ex3)
            {
                NotifyMessage("Disconnect failed.");
                //Log.Write(LogLevel.Debug, "TestSignalHandler::Disconnect()", ex3);
            }
        }

        private void RemoveAllSignals()
        {
            _commandExecuted.Reset();
            WriteRemoveAllSignals(LogSrvCommand.RemoveAllSignals);
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                throw new TimeoutException("RemoveAllSignals");
            }
        }

        protected override void OnAllSignalsRemoved(int status)
        {
            lock (_definedSignals)
            {
                _definedSignals.Clear();
                _orderedSignals.Clear();
            }
            lock (_trigs)
            {
                _trigs.Clear();
            }
            _commandExecuted.Set();
        }

        protected override void OnError(string errorMessage)
        {
            EventHandler<NotifyEventArgs<string>> eventHandler = this.Notification;
            if (eventHandler != null)
            {
                eventHandler(this, new NotifyEventArgs<string>(errorMessage));
            }
        }

        protected override void OnException(Exception ex)
        {
            Log.Write(LogLevel.Error, "TestSignalHandler::OnException", ex);
            Disconnect();
        }

        public void StartStopLog(LogSrvCommand cmd)
        {
            //Log.Write(LogLevel.Debug, "TestSignalHandler::StartStopLog", string.Format("{0} TestSignalHandler!", cmd));
            try
            {
                _commandExecuted.Reset();
                var channels = _definedSignals.Keys.ToArray();
                WriteStartStopLog(cmd, channels);
                if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
                {
                    //Log.Write(LogLevel.Debug, "TestSignalHandler::StartStopLog", "Timeout!");
                    throw new TimeoutException("StartStopLog");
                }
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Debug, "TestSignalHandler::StartStopLog", ex);
                throw;
            }
            finally
            {
                //Log.Write(LogLevel.Debug, "TestSignalHandler::StartStopLog", "Finished!");
            }
        }

        protected override void OnLogStartedStopped(LogSrvCommand cmd)
        {
            _commandExecuted.Set();
            _subscribing = cmd == LogSrvCommand.StartLog;
            SubscribingChanged?.Invoke(this, new NotifyEventArgs<bool>(true));
        }

        private double GetMinSampleTime()
        {
            double num = -1.0;
            if (_definedSignals is null)
            {
                return num;
            }

            foreach (LogSrvSignalDefinition value in _definedSignals.Values)
            {
                if (value.SampleTime < num || num == -1.0)
                {
                    num = value.SampleTime;
                }
            }

            return num;
        }

        private void PrepareLogDataBuffer()
        {
            foreach (int key in _definedSignals.Keys)
            {
                ReceiveLogDataObject receiveLogDataObject = new();
                if (AntiAliasFiltering)
                {
                    double sampleTime = _definedSignals[key].SampleTime;
                    int num = (int)Math.Round(SampleTime / sampleTime, 0);
                    Filter antiAliasFilter = null;
                    if (num > 1)
                    {
                        antiAliasFilter = new Cheby1Filter(num);
                    }
                    receiveLogDataObject.AntiAliasFilter = antiAliasFilter;
                }
                receiveLogDataObject.LoggedData = new();
                receiveLogDataObject.LoggedSamples = 0;
                receiveLogDataObject.ReceivedLogData = 0;
                _dataLogs.Add(key, receiveLogDataObject);
            }
        }

        protected override void OnLogData(int channel, int count, ReadDataBuffer dataBuffer)
        {
            if (!_definedSignals.ContainsKey(channel))
            {
                return;
            }

            List<double> list = new(count);
            if (_definedSignals[channel].Format == LogSrvSignalDefinition.LogSvrValueFormat.Float)
            {
                for (int i = 0; i < count; i++)
                {
                    list.Add(dataBuffer.ReadFloat());
                }
            }
            else if (_definedSignals[channel].Format == LogSrvSignalDefinition.LogSvrValueFormat.String || _definedSignals[channel].Format == LogSrvSignalDefinition.LogSvrValueFormat.Undefined)
            {
                Log.Write(LogLevel.Error, "TestSignalHandler::OnLogData", "Got format " + _definedSignals[channel].Format.ToString() + " and we don't have a way to handle it..");
            }
            else
            {
                for (int j = 0; j < count; j++)
                {
                    list.Add(dataBuffer.ReadInt());
                }
            }

            ReceiveLogDataObject receiveLogDataObject = _dataLogs[channel];
            List<double> loggedData = receiveLogDataObject.LoggedData;
            LogSrvSignalDefinition logSrvSignalDefinition = _definedSignals[channel];
            Trig trig = null;
            if (_trigs.ContainsKey(channel))
            {
                trig = _trigs[channel];
            }

            double sampleTime = logSrvSignalDefinition.SampleTime;
            int count2 = loggedData.Count;
            foreach (double item in list)
            {
                if (AntiAliasFiltering)
                {
                    if ((int)Math.Round(SampleTime / sampleTime, 0) > 1)
                    {
                        Filter antiAliasFilter = receiveLogDataObject.AntiAliasFilter;
                        antiAliasFilter.Execute(item);
                        if (antiAliasFilter.SampleFinished)
                        {
                            TrigHandler.AddNewSample(loggedData, antiAliasFilter.Value, trig);
                        }
                    }
                    else
                    {
                        if (loggedData.Count == 0)
                        {
                            TrigHandler.AddNewSample(loggedData, item, trig);
                            TrigHandler.AddNewSample(loggedData, item, trig);
                        }
                        TrigHandler.AddNewSample(loggedData, item, trig);
                    }
                    continue;
                }
                double minSampleTime = GetMinSampleTime();
                double num = sampleTime / minSampleTime;
                int num2 = (int)Math.Round(num, 0);
                if (Math.Abs(num2 - num) > 0.1)
                {
                    int num3 = 2;
                    while (Math.Abs(num * num2 - Math.Floor(num * num2)) > 0.1)
                    {
                        num3++;
                    }
                    int receivedLogData = receiveLogDataObject.ReceivedLogData;
                    receivedLogData++;
                    if (receivedLogData > num3)
                    {
                        receivedLogData = 1;
                    }
                    receiveLogDataObject.ReceivedLogData = receivedLogData;
                    int num4 = loggedData.Count % (int)((double)num3 * num);
                    num2 = (int)Math.Floor((double)receivedLogData * num);
                    if (num4 > 0)
                    {
                        num2 -= num4;
                    }
                }
                if (num2 <= 0)
                {
                    continue;
                }
                double num5 = item;
                if (loggedData.Count > 0)
                {
                    num5 = loggedData[loggedData.Count - 1];
                }
                for (int k = 0; k < num2; k++)
                {
                    if (ZeroOrderHold)
                    {
                        TrigHandler.AddNewSample(loggedData, item, trig);
                    }
                    else
                    {
                        TrigHandler.AddNewSample(loggedData, num5 + (item - num5) / (double)num2 * (double)(k + 1), trig);
                    }
                }
            }
            receiveLogDataObject.LoggedSamples += loggedData.Count - count2;
            list.Clear();
            if (MaxLogTime > 0)
            {
                int num6 = (int)((double)MaxLogTime / GetSampleTime()) + 1;
                if (loggedData.Count > num6)
                {
                    int num7 = loggedData.Count - num6;
                    foreach (ReceiveLogDataObject value in _dataLogs.Values)
                    {
                        if (value.LoggedData != null && value.LoggedData.Count > num7)
                        {
                            for (int l = 0; l < num7; l++)
                            {
                                value.LoggedData.RemoveAt(0);
                            }
                        }
                    }
                }
            }
            if (this.TestSignalRecived != null && channel == _lastChannel)
            {
                this.TestSignalRecived(this, new EventArgs());
            }
        }

        public List<List<double>> GetLog()
        {
            if (_dataLogs != null && _dataLogs.Count > 0)
            {
                List<List<double>> list = new List<List<double>>(_orderedSignals.Count + 1);
                using (Dictionary<int, ReceiveLogDataObject>.ValueCollection.Enumerator enumerator = _dataLogs.Values.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        ReceiveLogDataObject current = enumerator.Current;
                        int count = current.LoggedData.Count;
                        double sampleTime = GetSampleTime();
                        int loggedSamples = current.LoggedSamples;
                        List<double> list2 = new List<double>(count);
                        for (int i = 0; i < count; i++)
                        {
                            list2.Add((double)(loggedSamples - count + i) * sampleTime);
                        }
                        list.Add(list2);
                    }
                }
                if (_orderedSignals.Count == _definedSignals.Count || _orderedSignals.Count == 0)
                {
                    foreach (ReceiveLogDataObject value in _dataLogs.Values)
                    {
                        list.Add(value.LoggedData);
                    }
                }
                else
                {
                    for (int j = 0; j < _orderedSignals.Count; j++)
                    {
                        List<double> item = null;
                        foreach (int key in _definedSignals.Keys)
                        {
                            if (_definedSignals[key].MechName == _orderedSignals[j].MechUnit && _definedSignals[key].AxisNo == _orderedSignals[j].Axis - 1 && _definedSignals[key].SignalNo == _orderedSignals[j].TestSignal && _dataLogs.ContainsKey(key))
                            {
                                item = _dataLogs[key].LoggedData;
                            }
                        }
                        list.Add(item);
                    }
                }
                return list;
            }
            return null;
        }

        public double GetActiveLogTime()
        {
            double sampleTime = GetSampleTime();
            int num = 0;
            using (Dictionary<int, ReceiveLogDataObject>.ValueCollection.Enumerator enumerator = _dataLogs.Values.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    num = enumerator.Current.LoggedSamples;
                }
            }
            return sampleTime * (double)(num - 1);
        }

        private double GetSampleTime()
        {
            return (AntiAliasFiltering ? SampleTime : GetMinSampleTime()) * 0.001;
        }

        public int GetNumberOfSignals()
        {
            return _definedSignals.Count;
        }

        public void DefineSignal(string mechUnitName, int axisNumber, int signalNumber, double sampleTime, out int channel)
        {
            if (!CanAddChannel())
            {
                throw new Exception(string.Format("Cannot add more channels, max is {0}.", base.MAX));
            }

            channel = FindChannel(signalNumber, mechUnitName, axisNumber, sampleTime);
            if (!SignalDefined(signalNumber, mechUnitName, axisNumber, sampleTime))
            {
                _orderedSignals.Add(new Signal(mechUnitName, axisNumber, signalNumber));
                _commandExecuted.Reset();
                WriteDefineSignal(channel, signalNumber, mechUnitName, axisNumber, (float)sampleTime);
                if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
                {
                    throw new TimeoutException("DefineSignal");
                }
            }
        }

        private bool CanAddChannel()
        {
            return _definedSignals.Count < MAX_SIGNALS_AMOUNT;
        }

        private bool SignalDefined(int signalNumber, string mechUnitName, int axisNumber, double sampleTime)
        {
            foreach (LogSrvSignalDefinition value in _definedSignals.Values)
            {
                if (value.SignalNo == signalNumber && value.MechName == mechUnitName && value.AxisNo == axisNumber && value.SampleTime == sampleTime)
                {
                    return true;
                }
            }
            return false;
        }

        private int FindChannel(int signalNumber, string mechUnitName, int axisNumber, double sampleTime)
        {
            var channel = _definedSignals.FirstOrDefault(s => s.Value.SignalNo == signalNumber && s.Value.MechName == mechUnitName && s.Value.AxisNo == axisNumber && s.Value.SampleTime == sampleTime);

            int num = MAX_SIGNALS_AMOUNT - 1;
            foreach (var signal in _definedSignals.Values)
            {
                if (signal.SignalNo == signalNumber && signal.MechName == mechUnitName && signal.AxisNo == axisNumber && signal.SampleTime == sampleTime)
                {
                    return num;
                }
                num--;
            }
            return num;
        }

        protected override void OnSignalDefined(int channel, LogSrvSignalDefinition signal)
        {
            lock (_definedSignals)
            {
                _definedSignals.Add(channel, signal);
                _lastChannel = channel;
            }
            _commandExecuted.Set();
        }

        protected override void OnSignalRemoved(int status)
        {
            _commandExecuted.Set();
        }

        protected override void OnSignalsEnumerated(LogSrvSignalDefinition[] signals)
        {
            lock (_definedSignals)
            {
                _definedSignals.Clear();
                for (int i = 0; i < signals.Length; i++)
                {
                    if (signals[i].Enabled)
                    {
                        _definedSignals.Add(i, signals[i]);
                    }
                }
            }
            _commandExecuted.Set();
        }

        private bool ValidateConnectionState(ConnectionState requiredConnectionState)
        {
            if (_connectionState == requiredConnectionState)
            {
                return true;
            }
            Log.Write(LogLevel.Error, "", "this.connectionState == " + _connectionState.ToString() + " - requiredConnectionState == " + requiredConnectionState);
            NotifyMessage(string.Format("Invalid connection state ({0}).", _connectionState));
            return false;
        }

        private void NotifyMessage(string message)
        {
            EventHandler<NotifyEventArgs<string>> eventHandler = this.Notification;
            if (eventHandler != null)
            {
                eventHandler(this, new NotifyEventArgs<string>(message));
            }
        }

        private void SetConnectionState(ConnectionState connectionState)
        {
            ConnectionState connectionState2 = _connectionState;
            bool flag = connectionState2 == ConnectionState.Connected;
            _connectionState = connectionState;
            if (_connectionState != connectionState2 && this.ConnectionStateChanged != null)
            {
                this.ConnectionStateChanged(this, new PropertyChangedEventArgs<ConnectionState>(connectionState2, _connectionState));
            }
            bool flag2 = _connectionState == ConnectionState.Connected;
            if (flag2 != flag && this.ConnectedChanged != null)
            {
                this.ConnectedChanged(this, new NotifyEventArgs<bool>(flag2));
            }
        }

        private void ConnectCallback(IAsyncResult asyncResult)
        {
            try
            {
                ((Socket)asyncResult.AsyncState).EndConnect(asyncResult);
                _connectionComplete.Set();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        //DataSubscriptionClient Init
        void ITestDataSubscriptionClient.Connect()
        {
            try
            {
                //ConnectionClient ConnectAsync
                Connect();
                RemoveAllSignals();
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Error, "TestSignalHandler::Connect()", ex);
                Disconnect();
                throw;
            }
        }

        void ITestDataSubscriptionClient.AddSubscriber(IDataSubscriber subscriber)
        {
            double sampleTime = subscriber.SampleTime;
            var subscriptionData = subscriber.GetSubscriptionData();
            try
            {
                int channel;
                DefineSignal(subscriptionData.MechUnitName, subscriptionData.AxisNo, subscriptionData.SignalNo, sampleTime, out channel);
                _trigs.Add(channel, subscriptionData.Trig);
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Error, string.Format("TestSignalHandler::AddSubscriber({0} {1} {2})", subscriptionData.SignalNo, subscriptionData.MechUnitName, subscriptionData.AxisNo), ex);
            }

        }

        void ITestDataSubscriptionClient.StartSubscription()
        {
            StartStopLog(LogSrvCommand.StartLog);
        }

        void ITestDataSubscriptionClient.StopSubscription()
        {
            StartStopLog(LogSrvCommand.StopLog);
        }

        void ITestDataSubscriptionClient.Disconnect()
        {
            Disconnect();
        }

        void IMeasurementProviderHandler.Start()
        {
            ((ITestDataSubscriptionClient)this).StartSubscription();
        }

        void IMeasurementProviderHandler.Pause()
        {
            ((ITestDataSubscriptionClient)this).StopSubscription();
        }

        void IMeasurementProviderHandler.Stop()
        {
            ((ITestDataSubscriptionClient)this).StopSubscription();
            ((ITestDataSubscriptionClient)this).Disconnect();
        }

        bool IMeasurementProviderHandler.Init(IMeasurementsProvider[] providers)
        {
            bool result = false;
            try
            {
                ((ITestDataSubscriptionClient)this).Connect();
                if (providers != null)
                {
                    for (int i = 0; i < providers.Length; i++)
                    {
                        if (providers[i] is IDataSubscriber testSignalSubscriber)
                        {
                            ((ITestDataSubscriptionClient)this).AddSubscriber(testSignalSubscriber);
                        }
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                //Log.Write(LogLevel.Error, "TestSignalHandler::Init()", ex);
            }

            if (providers != null)
            {
                PrepareLogDataBuffer();
            }
            return result;
        }
    }
}
