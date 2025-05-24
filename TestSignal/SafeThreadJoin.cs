
namespace TestSignal
{
	//UI Class

	//public class SafeThreadJoin
	//{
	//	public static void DoEvents()
	//	{
	//		DispatcherFrame dispatcherFrame = new DispatcherFrame();
	//		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), dispatcherFrame);
	//		Dispatcher.PushFrame(dispatcherFrame);
	//	}

	//	private static object ExitFrame(object f)
	//	{
	//		((DispatcherFrame)f).Continue = false;
	//		return null;
	//	}

	//	public static void JoinWith(Thread thread, int pollRate)
	//	{
	//		if (Thread.CurrentThread != Dispatcher.CurrentDispatcher.Thread)
	//		{
	//			thread.Join();
	//			return;
	//		}
	//		while (!thread.Join(pollRate))
	//		{
	//			DoEvents();
	//		}
	//	}
	//}
}
