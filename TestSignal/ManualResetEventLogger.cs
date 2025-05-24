namespace TestSignal
{
	public class ManualResetEventLogger
	{
		private readonly ManualResetEvent cmd;

		private readonly string name = string.Empty;

		public ManualResetEventLogger(string name)
		{
			this.name = name;
			cmd = new ManualResetEvent(false);
		}

		public void Set()
		{
			cmd.Set();
			Log.Write(LogLevel.Debug, "ManualResetEvent::Set", name + "Set()");
		}

		public void Reset()
		{
			cmd.Reset();
			Log.Write(LogLevel.Debug, "ManualResetEvent::Reset", name + "Reset()");
		}

		public bool WaitOne(int timeout, bool locked)
		{
			bool result = cmd.WaitOne(timeout, locked);
			Log.Write(LogLevel.Debug, "ManualResetEvent::WaitOne", name + "WaitOne() Got: " + result);
			return result;
		}
	}
}
