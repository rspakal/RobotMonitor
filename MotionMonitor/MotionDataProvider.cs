using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class MotionDataProvider
    {
        private ManualResetEvent _commandExecuted;
        public readonly DataStreamHandler StreamHandler;
        public readonly Signal[] OrderedSignals;
        public Dictionary<int, Signal> SubscribedSignals { get; set; }
        public MotionDataProvider()
        {
            StreamHandler = new DataStreamHandler(this);
            OrderedSignals = Signal.BuildOrderedSignals();
            SubscribedSignals = new();
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
