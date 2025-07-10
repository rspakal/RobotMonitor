namespace MotionMonitor
{
	public class ReceiveLogDataObject
	{
		public int ChannelNo { get; set; }
		public List<double>? LoggedDataValues { get; set; }
		public int LoggedSamples { get; set; }
		public int ReceivedLogData { get; set; }
		public Filter? AntiAliasFilter { get; set; }
	}
}
