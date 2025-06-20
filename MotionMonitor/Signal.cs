using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionMonitor
{
    public class Signal
    {
        public enum LogSvrValueFormat
        {
            Undefined,
            Bool,
            Short,
            Int,
            Float,
            String
        }
        private const string ROBOT_NAME = "ROB_1";
        private const int VELOCITY_SIGNAL = 1717;
        private const int TORQUE_SIGNAL = 4947;
        public const int MOC_ID = 5;
        private const int MOC_ERR = 50000;
        public const int SYS_ERR_MOC_TEST_SIGNAL_ERROR = 133;
        public const int SYS_ERR_MOC_UNKNOWN_SIGNAL_NUMBER = 228;
        public const int SYS_ERR_MOC_UNKNOWN_MECH_UNIT_NAME = 229;
        public const int SYS_ERR_MOC_MECH_UNIT_NOT_ACTIVE = 231;
        public const int SYS_ERR_MOC_TEST_SIGNAL_OVERFLOW = 234;
        public const int SYS_ERR_MOC_TEST_SIGNAL_DEFINE_ERROR = 348;
        public const int SYS_ERR_MOC_TOO_MANY_CONTINUOUS_LOG_SIGNALS = 461;
        public const int SYS_ERR_MOC_LOGSRV_SOCKET_SEND_FAILED = 463;
        public const int LOGSRV_ERR_TAKEN_BY_ROBAPI_CLIENT = -301;
        public const int LOGSRV_ERR_TAKEN_BY_EXTERNAL_CLIENT = -302;
        public const int LOGSRV_NO_JOINT_FOUND = -303;
        public const int LOGSRV_NO_ROBAPI_CLIENT_CONNECTED = -304;
        public const int LOGSRV_NO_SIG_FOUND = -50133;
        public const int LOGSRV_UNK_SIG_NUM = -50228;
        public const int LOGSRV_MECH_UNIT_UNKNOWN = -50229;
        public const int LOGSRV_NO_CHANNEL_AVAILABLE = -50348;
        public const int LOGSRV_NOT_INSTALLED = -50461;
        private static double _axcSampleTime = 0.000504;
        private readonly int _axisNo;
        private int _signalNo;
        private readonly string _robName;
        private float _sampleTime;
        //private Trig _trig;
        private static readonly List<object> lineColors = new List<object>();
        private static readonly List<Color> standardColors = new List<Color>();

        public static double AxcSampleTime
        {
            get => _axcSampleTime;
        }

        public int Axis => _axisNo;
        public int SignalNo
        {
            get => _signalNo;
            set => _signalNo = value;
        }
        public string RobName => _robName;
        //public Trig Trig
        //{
        //    get => _trig;
        //    set => _trig = value;
        //}

        public Signal(string robName, int axisNo, int signalNo, float sampleTime = 0)
        {
            _robName = robName;
            _axisNo = axisNo;
            _signalNo = signalNo;
            _sampleTime = sampleTime;
        }

        public static Signal[] BuildOrderedSignals()
        {
            Signal[] signals = new Signal[12];
            int signal;
            for (int i = 0; i < 12; i++)
            {
                signal = i < 6 ? VELOCITY_SIGNAL : TORQUE_SIGNAL;
                signals[i] = new Signal(ROBOT_NAME, i + 1, signal);
            }
            return signals;
        }
        public static Signal BuildSignal(ReadDataBuffer buffer)
        {
            var signalNo = buffer.ReadInt();
            var robotName = buffer.ReadString(40);
            var axisNo = buffer.ReadInt();
            var format = (LogSvrValueFormat)buffer.ReadInt();
            var sampleTime = buffer.ReadFloat();
            var blockSize = buffer.ReadInt();
            return new(robotName, axisNo, signalNo, sampleTime);
        }
    }
}
