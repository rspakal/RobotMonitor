using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class DataProvider
    {
        private ManualResetEvent _commandExecuted;
        public readonly DataStreamHandler StreamHandler;
        public readonly Signal[] OrderedSignals;
        public Dictionary<int, Signal> SubscribedSignals { get; set; }

        private const int StoMS = 1000;
        private readonly int _signalNo;
        private readonly int _axisNo;
        private readonly double _sampleTime;
        private readonly string _mechUnitName;
        private readonly bool _antiAliasFiltering;
        private readonly Trig trig;
        public DataProvider(string mechUnitName, int signalNo, int axisNo, double sampleTime, bool antiAliasFiltering, Trig trig)
        {
            _mechUnitName = mechUnitName;
            _signalNo = signalNo;
            _axisNo = axisNo;
            _sampleTime = sampleTime;
            _antiAliasFiltering = antiAliasFiltering;
            if (_antiAliasFiltering)
            {
                _sampleTime = Signal.AxcSampleTime * 1000.0;
            }
            this.trig = trig;
        }

        public void DefineSignal(int signalNumber, string mechUnitName, int axisNumber, double sampleTime, out int channel)
        {
            if (!CanAddChannel())
            {
                throw new Exception(string.Format("Cannot add more channels, max is {0}.", base.MaxNoSignals));
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
        public void RemoveAllSignals()
        {
            _commandExecuted.Reset();
            WriteRemoveAllSignals();
            if (!_commandExecuted.WaitOne(COMMAND_TIMEOUT, true))
            {
                throw new TimeoutException("RemoveAllSignals");
            }
        }
        public void OnSignalDefined(ReadDataBuffer dataBuffer)
        {
            var channel = dataBuffer.ReadInt();
            var signal = Signal.BuildSignal(dataBuffer);
            lock (SubscribedSignals)
            {
                SubscribedSignals.Add(channel, signal);
                _lastChannel = channel;
            }
            _commandExecuted.Set();
        }
        public void OnAllSignalsRemoved(ReadDataBuffer dataBuffer)
        {
            int status = dataBuffer.ReadInt();
            lock (SubscribedSignals)
            {
                SubscribedSignals.Clear();
            }
            //lock (_trigs)
            //{
            //    _trigs.Clear();
            //}
            _commandExecuted.Set();
        }
    }
}
