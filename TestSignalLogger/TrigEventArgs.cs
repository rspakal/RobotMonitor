namespace TestSignalLogger
{
	public class TrigEventArgs : EventArgs
	{
		public double StopTime { get; set; }
		public bool RestartLog { get; set; }
	}
}
