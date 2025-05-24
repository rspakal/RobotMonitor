namespace TestSignal
{
	public class Log
	{
		private static readonly Mutex mutex = new Mutex();
		private static LogLevel level = LogLevel.Error;
		private static Stream? output = null;
		private static StreamWriter? writer = null;

		private static bool LevelCheck(LogLevel level)
		{
			return Log.level >= level;
		}

		public static void SetLogLevel(LogLevel level)
		{
			mutex.WaitOne();
			Log.level = level;
			mutex.ReleaseMutex();
		}

		public static void SetLogLevel(int level)
		{
			switch (level)
			{
				case 0:
					SetLogLevel(LogLevel.None);
					break;
				case 1:
					SetLogLevel(LogLevel.Error);
					break;
				case 2:
					SetLogLevel(LogLevel.Warning);
					break;
				case 3:
					SetLogLevel(LogLevel.Information);
					break;
				case 4:
					SetLogLevel(LogLevel.Debug);
					break;
				default:
					SetLogLevel(LogLevel.Debug);
					break;
			}
		}

		public static void SetLogOutput(Stream output)
		{
			mutex.WaitOne();
			if (output.CanWrite)
			{
				Log.output = output;
				writer = new StreamWriter(Log.output);
				writer.AutoFlush = true;
			}
			mutex.ReleaseMutex();
		}

		public static void Write(LogLevel level, string source, string message)
		{
			mutex.WaitOne();
			try
			{
				if (LevelCheck(level) && writer != null)
				{
					writer.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} {1} {2} {3}", DateTime.Now, level.ToString(), source, message));
				}
			}
			catch
			{
			}
			mutex.ReleaseMutex();
		}

		public static void Write(LogLevel level, string source, Exception ex)
		{
			mutex.WaitOne();
			try
			{
				if (LevelCheck(level) && writer != null)
				{
					writer.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} {1} {2} {3}", DateTime.Now, level.ToString(), source, ex.Message));
					Exception innerException = ex.InnerException;
					if (innerException != null)
					{
						writer.WriteLine(string.Format("\t--> {0} {1}", innerException.Source, innerException.Message));
					}
				}
			}
			catch
			{
			}
			mutex.ReleaseMutex();
		}
	}
}
