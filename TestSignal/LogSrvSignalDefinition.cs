namespace TestSignal
{
	public class LogSrvSignalDefinition
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

		private const int MECH_UNIT_NAME_LENGTH = 40;
		internal const int LOGSRV_SIGNAL_DEFINITION_MESSAGE_SIZE = 60;
		private readonly int _signalNo;
		private readonly string _mechName;
		private readonly int _axisNo;
		private readonly LogSvrValueFormat _format;
		private readonly double _sampleTime;
		private readonly int _blockSize;
		private bool _enabled;

		public bool Enabled
		{
			get => _enabled;
			set => _enabled = value;
		}
		public int SignalNo => _signalNo;
		public string MechName => _mechName;
		public int AxisNo => _axisNo;
		public LogSvrValueFormat Format => _format;
		public double SampleTime => _sampleTime;
		public int BlockSize => _blockSize;

		internal LogSrvSignalDefinition(LogSrvSignalDefinition other)
		{
			_signalNo = other.SignalNo;
			_mechName = other.MechName;
			_axisNo = other.AxisNo;
			_format = other.Format;
			_sampleTime = other.SampleTime;
			_blockSize = other.BlockSize;
			_enabled = other.Enabled;
		}

		internal LogSrvSignalDefinition(ReadDataBuffer buffer)
		{
			_signalNo = buffer.ReadInt();
			_mechName = buffer.ReadString(40);
			_axisNo = buffer.ReadInt();
			_format = (LogSvrValueFormat)buffer.ReadInt();
			_sampleTime = buffer.ReadFloat();
			_blockSize = buffer.ReadInt();
			_enabled = _signalNo != 0;
		}
	}
}
