using TestSignalLogger;
namespace TestSignal
{
	public class ReceiveLogDataObject
	{
		public List<double>? LoggedData { get; set; }

		public int LoggedSamples { get; set; }

		public int ReceivedLogData { get; set; }

		public Filter? AntiAliasFilter { get; set; }
	}
}
