using System.Drawing;
namespace TestSignalLogger
{
    [Serializable]
	public class Signal
	{
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
		private static double axcSampleTime = 0.000504;
		private readonly int axis;
		private int signal;
		private readonly string mechunit;
		private Trig trig;
		private static readonly List<object> lineColors = new List<object>();
		private static readonly List<Color> standardColors = new List<Color>();

		public static double AxcSampleTime
		{
			get
			{
				return axcSampleTime;
			}
			set
			{
				if (value > 1E-06)
				{
					axcSampleTime = value;
				}
			}
		}

		public int Axis
		{
			get
			{
				return axis;
			}
		}

		public int TestSignal
		{
			get
			{
				return signal;
			}
		}

		public string MechUnit
		{
			get
			{
				return mechunit;
			}
		}

		public Trig Trig
		{
			get
			{
				return trig;
			}
			set
			{
				trig = value;
			}
		}

		public Signal(string mechunit, int axis, int signal)
		{
			this.axis = axis;
			this.signal = signal;
			this.mechunit = mechunit;
		}

		public static Color GetStandardColor(int colorIndex)
		{
			Color result = Color.Gray;
			while (colorIndex >= standardColors.Count)
			{
				switch (standardColors.Count)
				{
					case 0:
						standardColors.Add(ColorsABB.StatusRed);
						continue;
					case 1:
						standardColors.Add(ColorsABB.StatusBlue);
						continue;
					case 2:
						standardColors.Add(ColorsABB.StatusGreen);
						continue;
					case 3:
						standardColors.Add(ColorsABB.StatusCyan);
						continue;
					case 4:
						standardColors.Add(ColorsABB.StatusYellow);
						continue;
					case 5:
						standardColors.Add(ColorsABB.StatusOrange);
						continue;
					case 6:
						standardColors.Add(ColorsABB.StatusMangenta);
						continue;
					case 7:
						standardColors.Add(Color.YellowGreen);
						continue;
					case 8:
						standardColors.Add(Color.CadetBlue);
						continue;
					case 9:
						standardColors.Add(Color.Purple);
						continue;
					case 10:
						standardColors.Add(Color.Crimson);
						continue;
					case 11:
						standardColors.Add(Color.Brown);
						continue;
					case 12:
						standardColors.Add(Color.SkyBlue);
						continue;
					case 13:
						standardColors.Add(Color.LightGreen);
						continue;
					case 14:
						standardColors.Add(Color.IndianRed);
						continue;
					case 15:
						standardColors.Add(Color.DarkBlue);
						continue;
					case 16:
						standardColors.Add(Color.DarkGreen);
						continue;
					case 17:
						standardColors.Add(Color.DarkRed);
						continue;
				}
				int num = 0;
				int num2 = 180;
				KnownColor[] array = (KnownColor[])Enum.GetValues(typeof(KnownColor));
				int num3 = array.Length;
				Color item;
				do
				{
					num++;
					Random random = new Random(standardColors.Count + num);
					item = Color.FromKnownColor(array[random.Next(array.Length)]);
				}
				while ((num <= num3 && standardColors.Contains(item)) || (item.R >= num2 && item.G >= num2 && item.B >= num2));
				if (!standardColors.Contains(item))
				{
					standardColors.Add(item);
				}
			}
			if (colorIndex < standardColors.Count)
			{
				if (colorIndex < 0)
				{
					colorIndex = 0;
				}
				result = standardColors[colorIndex];
			}
			return result;
		}

		public static void SetStandardColor(int colorIndex, Color c)
		{
			if (colorIndex < standardColors.Count)
			{
				standardColors[colorIndex] = c;
			}
		}

		public static Color GetColor(int colorIndex)
		{
			Color result = GetStandardColor(colorIndex);
			if (colorIndex < lineColors.Count && lineColors[colorIndex] != null)
			{
				result = (Color)lineColors[colorIndex];
			}
			return result;
		}

		public static void SetColor(int colorIndex, Color c)
		{
			while (colorIndex >= lineColors.Count)
			{
				lineColors.Add(null);
			}
			lineColors[colorIndex] = c;
		}

		public void SetSignalNumber(int signal)
		{
			this.signal = signal;
		}
	}
}
