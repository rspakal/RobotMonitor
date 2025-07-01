using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSignal
{
    public enum LogSvrMessage
    {
        LogData = 7,
        SignalDefined = 51,
        LoggingStarted = 52,
        LoggingStopped = 53,
        SignalRemoved = 54,
        AllSignalsRemoved = 55,
        SignalsEnumerated = 56,
        Error = 60
    }
}
