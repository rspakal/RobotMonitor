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

		private const int LOGSRV_MECH_UNIT_NAME_LENGTH = 40;

		internal const int LOGSRV_SIGNAL_DEFINITION_MESSAGE_SIZE = 60;

		private readonly int signalNo;

		private readonly string mechName;

		private readonly int axisNo;

		private readonly LogSvrValueFormat format;

		private readonly double sampleTime;

		private readonly int blockSize;

		private bool enabled;

		public bool Enabled
		{
			get
			{
				return enabled;
			}
			set
			{
				enabled = value;
			}
		}

		public int SignalNo
		{
			get
			{
				return signalNo;
			}
		}

		public string MechName
		{
			get
			{
				return mechName;
			}
		}

		public int AxisNo
		{
			get
			{
				return axisNo;
			}
		}

		public LogSvrValueFormat Format
		{
			get
			{
				return format;
			}
		}

		public double SampleTime
		{
			get
			{
				return sampleTime;
			}
		}

		public int BlockSize
		{
			get
			{
				return blockSize;
			}
		}

		internal LogSrvSignalDefinition(LogSrvSignalDefinition other)
		{
			signalNo = other.signalNo;
			mechName = other.mechName;
			axisNo = other.axisNo;
			format = other.format;
			sampleTime = other.sampleTime;
			blockSize = other.blockSize;
			enabled = other.enabled;
		}

		internal LogSrvSignalDefinition(ReverseOrderByteBuffer buffer)
		{
			signalNo = buffer.ReadInt32();
			mechName = buffer.ReadString(40);
			axisNo = buffer.ReadInt32();
			format = (LogSvrValueFormat)buffer.ReadInt32();
			sampleTime = buffer.ReadSingle();
			blockSize = buffer.ReadInt32();
			enabled = signalNo != 0;
		}
	}
}
