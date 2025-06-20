using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class NotifyEventArgs<T> : EventArgs
    {
        private readonly T _value;
        public T Value => _value;
        public NotifyEventArgs(T value) => _value = value;
    }
}
