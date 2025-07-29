using MotionMonitor.Enums;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestSignalLogger;
namespace TestSignal
{
    public class TestSignalLogData : IDisposable
    //public class TestSignalLogData : ITestSignalLogger, IDisposable
	{
		[StructLayout(LayoutKind.Auto)]
		[CompilerGenerated]
		private struct _003CStartLog_003Ed__21 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncTaskMethodBuilder<bool> _003C_003Et__builder;
			public TestSignalLogData _003C_003E4__this;
			public double sampleTime;
			public bool antiAliasFiltering;
			public bool zeroOrderHold;
			public int maxLogTime;
			public Signal[] signals;
			private TaskAwaiter _003C_003Eu__1;

			private void MoveNext()
			{
				int num = _003C_003E1__state;
				TestSignalLogData testSignalLogData = _003C_003E4__this;
				bool result;
				try
				{
					TaskAwaiter awaiter;
					if (num != 0)
					{
						if (num == 1)
						{
							awaiter = _003C_003Eu__1;
							_003C_003Eu__1 = default(TaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_02ad;
						}
						awaiter = Task.Run(delegate
						{
						}).GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							num = (_003C_003E1__state = 0);
							_003C_003Eu__1 = awaiter;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
							return;
						}
					}
					else
					{
						awaiter = _003C_003Eu__1;
						_003C_003Eu__1 = default(TaskAwaiter);
						num = (_003C_003E1__state = -1);
					}
					awaiter.GetResult();
					if (testSignalLogData.testSignalHandler != null && testSignalLogData.testSignalHandler.ConnectionState == ConnectionState.Idle)
					{
						List<DataProvider> testSignalProviders = testSignalLogData.testSignalProviders;
						if (testSignalProviders != null)
						{
							testSignalProviders.Clear();
						}
						double axcSampleTime = Signal.AxcSampleTime;
						if (sampleTime <= 0.0)
						{
							sampleTime = axcSampleTime;
						}
						sampleTime = (sampleTime / axcSampleTime) * (axcSampleTime + 5E-07);
						sampleTime *= 1000.0;
						testSignalLogData.testSignalHandler.AntiAliasFiltering = antiAliasFiltering;
						testSignalLogData.testSignalHandler.SampleTime = sampleTime;
						testSignalLogData.testSignalHandler.ZeroOrderHold = zeroOrderHold;
						testSignalLogData.testSignalHandler.MaxLogTime = maxLogTime;
						if (testSignalLogData.testSignalProviders == null)
						{
							testSignalLogData.testSignalProviders = new List<DataProvider>();
						}
						Signal[] array = signals;
						foreach (Signal signal in array)
						{
							testSignalLogData.testSignalProviders.Add(new DataProvider(signal.MechUnitName, signal.SignalNo, signal.AxisNo + 1, sampleTime, antiAliasFiltering, signal.Trig));
						}
						IMeasurementProviderHandler testSignalHandler = testSignalLogData.testSignalHandler;
						IMeasurementsProvider[] providers = testSignalLogData.testSignalProviders.ToArray();
						if (!testSignalHandler.Init(providers))
						{
							awaiter = testSignalLogData.StopLog().GetAwaiter();
							if (!awaiter.IsCompleted)
							{
								num = (_003C_003E1__state = 1);
								_003C_003Eu__1 = awaiter;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
								return;
							}
							goto IL_02ad;
						}
						List<DataProvider>.Enumerator enumerator = testSignalLogData.testSignalProviders.GetEnumerator();
						try
						{
							while (enumerator.MoveNext())
							{
								((IMeasurementsProvider)enumerator.Current).Start();
							}
						}
						finally
						{
							if (num < 0)
							{
								((IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
							}
						}
						testSignalHandler.Start();
						testSignalLogData.testSignalHandler.TestSignalRecived += testSignalLogData.OnLogDataReceived;
						testSignalLogData.testSignalHandler.TrigHandler.TrigActivated += testSignalLogData.OnTrigActivated;
					}
					result = true;
					goto end_IL_000e;
				IL_02ad:
					awaiter.GetResult();
					throw new COMException("Another Socket client is already connected, restart controller to reset connections.", -302);
				end_IL_000e:;
				}
				catch (Exception exception)
				{
					_003C_003E1__state = -2;
					_003C_003Et__builder.SetException(exception);
					return;
				}
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetResult(result);
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				_003C_003Et__builder.SetStateMachine(stateMachine);
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout(LayoutKind.Auto)]
		[CompilerGenerated]
		private struct _003CStopLog_003Ed__22 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncTaskMethodBuilder _003C_003Et__builder;

			public TestSignalLogData _003C_003E4__this;

			private TaskAwaiter _003C_003Eu__1;

			private void MoveNext()
			{
				int num = _003C_003E1__state;
				TestSignalLogData testSignalLogData = _003C_003E4__this;
				try
				{
					TaskAwaiter awaiter;
					if (num != 0)
					{
						awaiter = Task.Run(delegate
						{
						}).GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							num = (_003C_003E1__state = 0);
							_003C_003Eu__1 = awaiter;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
							return;
						}
					}
					else
					{
						awaiter = _003C_003Eu__1;
						_003C_003Eu__1 = default(TaskAwaiter);
						num = (_003C_003E1__state = -1);
					}
					awaiter.GetResult();
					if (testSignalLogData.testSignalHandler != null && testSignalLogData.testSignalHandler.ConnectionState == ConnectionState.Connected)
					{
						testSignalLogData.testSignalHandler.TestSignalRecived -= testSignalLogData.OnLogDataReceived;
						testSignalLogData.testSignalHandler.TrigHandler.TrigActivated -= testSignalLogData.OnTrigActivated;
						List<TestSignalProvider>.Enumerator enumerator = testSignalLogData.testSignalProviders.GetEnumerator();
						try
						{
							while (enumerator.MoveNext())
							{
								((IMeasurementsProvider)enumerator.Current).Stop();
							}
						}
						finally
						{
							if (num < 0)
							{
								((IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
							}
						}
						try
						{
							((IMeasurementProviderHandler)testSignalLogData.testSignalHandler).Stop();
						}
						catch (Exception ex)
						{
							throw ex;
						}
						finally
						{
							if (num < 0)
							{
								testSignalLogData.testSignalProviders.Clear();
							}
						}
					}
				}
				catch (Exception exception)
				{
					_003C_003E1__state = -2;
					_003C_003Et__builder.SetException(exception);
					return;
				}
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetResult();
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				_003C_003Et__builder.SetStateMachine(stateMachine);
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		private const int StoMS = 1000;
		private const int LOGSRV_MAX_BLOCK_SIZE = 192;
		private readonly int[] _sampleFactors = { 1, 2, 4, 8, 16, 32, 48, 64, 96, 192 };
		public TestSignalHandler testSignalHandler;
		public List<DataProvider> testSignalProviders = new();
		public string Name => "Socket";
		public double SampleTime => testSignalHandler.SampleTime;
		public double SampleTimeBase => Signal.AxcSampleTime;
		public int[] SampleFactors => _sampleFactors;

		public event EventHandler LogDataReceived;

		public event TrigEventHandler TrigActivated;

		public void Connect(IPAddress ipAddress)
		{
			testSignalHandler = new TestSignalHandler(ipAddress);
		}

		[AsyncStateMachine(typeof(_003CStartLog_003Ed__21))]
		public Task<bool> StartLog(Signal[] signals, double sampleTime, bool antiAliasFiltering, bool zeroOrderHold, int maxLogTime, uint latency)
		{
			_003CStartLog_003Ed__21 stateMachine = default(_003CStartLog_003Ed__21);
			stateMachine._003C_003Et__builder = AsyncTaskMethodBuilder<bool>.Create();
			stateMachine._003C_003E4__this = this;
			stateMachine.signals = signals;
			stateMachine.sampleTime = sampleTime;
			stateMachine.antiAliasFiltering = antiAliasFiltering;
			stateMachine.zeroOrderHold = zeroOrderHold;
			stateMachine.maxLogTime = maxLogTime;
			stateMachine._003C_003E1__state = -1;
			stateMachine._003C_003Et__builder.Start(ref stateMachine);
			return stateMachine._003C_003Et__builder.Task;
		}

		[AsyncStateMachine(typeof(_003CStopLog_003Ed__22))]
		public Task StopLog()
		{
			_003CStopLog_003Ed__22 stateMachine = default(_003CStopLog_003Ed__22);
			stateMachine._003C_003Et__builder = AsyncTaskMethodBuilder.Create();
			stateMachine._003C_003E4__this = this;
			stateMachine._003C_003E1__state = -1;
			stateMachine._003C_003Et__builder.Start(ref stateMachine);
			return stateMachine._003C_003Et__builder.Task;
		}

		public List<List<double>> GetLog()
		{
			return testSignalHandler.GetLog();
		}

		public double GetActiveLogTime()
		{
			return testSignalHandler.GetActiveLogTime();
		}

		public int GetNumberOfSignals()
		{
			return testSignalHandler.GetNumberOfSignals();
		}

		public void OnLogDataReceived(object sender, EventArgs e)
		{
			if (LogDataReceived != null)
			{
				LogDataReceived(sender, e);
			}
		}

		public void OnTrigActivated(object sender, TrigEventArgs e)
		{
			if (this.TrigActivated != null)
			{
				e.StopTime += testSignalHandler.GetActiveLogTime();
				this.TrigActivated(sender, e);
			}
		}

		public void Dispose()
		{
			if (testSignalHandler != null)
			{
				testSignalHandler.Dispose();
				testSignalHandler = null;
			}
		}
	}
}
