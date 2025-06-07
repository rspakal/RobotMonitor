using System.Net.Sockets;
namespace TestSignal
{
    public abstract class LogSrvStreamHandler
	{
		private enum LogSvrMessage
		{
			LogData = 7,
			SignalDefined = 51,
			LoggingStarted = 52,
			LoggingStopped = 53,
			SignalRemoved = 54,
			AllSignalsRemoved = 55,
			SignalsEnumerated = 56,
			Error = 60
		}

		private const int READ_BUFFER_LENGTH = 7712;
		private const int LOGSRV_MAX_ERROR_MESSAGE_SIZE = 80;
		private const int MAX_NO_EXTERNAL_SIGNALS = 12;
		private const int LOGSRV_MECHANICAL_UNIT_MAX_NAME_LENGTH = 40;
		private bool _reading;
		private NetworkStream? _networkStream;
		private Thread? _readThread;
		private int _sleepCounter;
		protected int MaxNoSignals => MAX_NO_EXTERNAL_SIGNALS;
		protected void StartStreamHandler(Socket socket, bool ownsSocket)
		{
			_networkStream = new NetworkStream(socket, ownsSocket)
			{
				ReadTimeout = 1000
			};
			_reading = true;
			_readThread = new Thread(Read)
			{
				Name = "LogSrvStreamHandler.readThread",
				Priority = ThreadPriority.AboveNormal
			};
			_readThread.Start();
		}

		protected virtual void OnSignalRemoved(int status)
		{
		}

		protected virtual void OnAllSignalsRemoved(int status)
		{
		}

		protected virtual void OnSignalDefined(int channel, LogSrvSignalDefinition signal)
		{
		}

		protected virtual void OnSignalsEnumerated(LogSrvSignalDefinition[] signals)
		{
		}

		protected virtual void OnError(string errorMessage)
		{
		}

		protected virtual void OnException(Exception ex)
		{
		}

		protected virtual void OnLogData(int channel, int count, ReverseOrderByteBuffer robb)
		{
		}

		protected virtual void OnLogStartedStopped(LogSrvCommand cmd)
		{
		}

		protected void StopStreamHandler()
		{
			_reading = false;
			_networkStream.Dispose();
			_networkStream = null;
			if (_readThread != null && _readThread.IsAlive)
			{
				Log.Write(LogLevel.Debug, "LogSrvStreamHandler::StopStreamHandler", "Joining readThread");
				//SafeThreadJoin.JoinWith(readThread, 1);
				Log.Write(LogLevel.Debug, "LogSrvStreamHandler::StopStreamHandler", "Joining complete for readThread");
			}
		}

		protected void WriteRemoveAllSignals()
		{
			ReverseByteOrderWriter reverseByteOrderWriter = new ReverseByteOrderWriter();
			reverseByteOrderWriter.Write(5);
			Write(reverseByteOrderWriter.GetBytes());
		}

		protected void WriteStartStopLog(LogSrvCommand cmd, int[] channels)
		{
			ReverseByteOrderWriter reverseByteOrderWriter = new ReverseByteOrderWriter();
			reverseByteOrderWriter.Write((int)cmd);
			WriteStartStopCmd(reverseByteOrderWriter, channels);
			OnLogStartedStopped(cmd);
		}

		private void WriteStartStopCmd(ReverseByteOrderWriter rbw, int[] channels)
		{
			rbw.Write(channels.Length);
			for (int i = 0; i < 12; i++)
			{
				if (i < channels.Length)
				{
					rbw.Write(channels[i]);
				}
				else
				{
					rbw.Write(-1);
				}
			}
			Write(rbw.GetBytes());
		}

		protected void WriteDefineSignal(int channel, int signalNumber, string mechUnitName, int axisNumber, float sampleTime)
		{
			if (channel < 0 || channel > 11)
			{
				throw new ArgumentOutOfRangeException("channel", channel, string.Format("Value of must be between 0 and {0}", 11));
			}
			if (axisNumber < 1 || axisNumber > 6)
			{
				throw new ArgumentOutOfRangeException("axisNumber", axisNumber, "Value of must be between 1 and 6");
			}
			ReverseByteOrderWriter reverseByteOrderWriter = new ReverseByteOrderWriter();
			reverseByteOrderWriter.Write(1);
			reverseByteOrderWriter.Write(channel);
			reverseByteOrderWriter.Write(signalNumber);
			reverseByteOrderWriter.Write(mechUnitName, 40);
			reverseByteOrderWriter.Write(axisNumber - 1);
			reverseByteOrderWriter.Write(sampleTime);
			Write(reverseByteOrderWriter.GetBytes());
		}

		protected void WriteRemoveSignal(int channel)
		{
			ReverseByteOrderWriter reverseByteOrderWriter = new ReverseByteOrderWriter();
			reverseByteOrderWriter.Write(4);
			reverseByteOrderWriter.Write(channel);
			Write(reverseByteOrderWriter.GetBytes());
		}

		protected void WriteEnumerateSignals()
		{
			ReverseByteOrderWriter reverseByteOrderWriter = new ReverseByteOrderWriter();
			reverseByteOrderWriter.Write(6);
			Write(reverseByteOrderWriter.GetBytes());
		}

		public void Write(byte[] data)
		{
			if (_networkStream == null || data == null)
			{
				return;
			}
			DateTime now = DateTime.Now;
			bool flag = false;
			do
			{
				try
				{
					_networkStream.Write(data, 0, data.Length);
					flag = true;
				}
				catch (Exception ex)
				{
					if ((DateTime.Now - now).TotalMilliseconds < 100.0 && ex.Message.Contains("Unable to write data to the transport connection"))
					{
						Thread.Sleep(1);
						continue;
					}
					throw;
				}
			}
			while (!flag);
		}

		private void Read()
		{
			byte[] array = new byte[7712];
			ReverseOrderByteBuffer reverseOrderByteBuffer = new ReverseOrderByteBuffer(array);
			int offset = 0;
			int num2 = 0;
			int bytesRead = 0;
			int index = 0;
			bool flag = false;

            while (_networkStream != null && (_reading || bytesRead > 0))
			{
				try
				{
					bytesRead = 0;
					if (_networkStream.DataAvailable)
					{
						bytesRead = _networkStream.Read(array, offset, 7712 - offset);
						num2 = offset + bytesRead;
						if (bytesRead > 0)
						{
							//Edited
							//num4 = Array.FindIndex(array, 0, num2 - offset, (byte b) => b != 0);
							index = Array.FindIndex(array, 0, bytesRead, b => b != 0);
							//------
							while (index >= 0 && !flag)
							{
								reverseOrderByteBuffer.CurrentIndex = index;
								reverseOrderByteBuffer.Skip(1);
								switch (array[index])
								{
									case 7:
										{
											if (index + 3856 - 3 > num2)
											{
												flag = true;
												break;
											}
											int count = reverseOrderByteBuffer.ReadInt32();
											int channel = reverseOrderByteBuffer.ReadInt32();
											reverseOrderByteBuffer.ReadInt32();
											OnLogData(channel, count, reverseOrderByteBuffer);
											break;
										}
									case 55:
										if (index + 4 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnAllSignalsRemoved(reverseOrderByteBuffer.ReadInt32());
										}
										break;
									case 60:
										if (index + 80 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnError(reverseOrderByteBuffer.ReadString(80));
										}
										break;
									case 51:
										if (index + 4 + 60 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnSignalDefined(reverseOrderByteBuffer.ReadInt32(), new LogSrvSignalDefinition(reverseOrderByteBuffer));
										}
										break;
									case 54:
										if (index + 4 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnSignalRemoved(reverseOrderByteBuffer.ReadInt32());
										}
										break;
									case 56:
										{
											if (index + 720 - 3 > num2)
											{
												flag = true;
												break;
											}
											LogSrvSignalDefinition[] array2 = new LogSrvSignalDefinition[12];
											for (int num6 = 0; num6 < 12; num6++)
											{
												array2[num6] = new LogSrvSignalDefinition(reverseOrderByteBuffer);
											}
											OnSignalsEnumerated(array2);
											break;
										}
								}
								if (flag)
								{
									//Edited
									//int num7 = num2 - index;
									//Array.Copy(array, index, array, 0, num7);
									//offset = num7;
									//flag = false;
									//break;

									offset = num2 - index;
									Array.Copy(array, index, array, 0, offset);
									flag = false;
									break;
									//-----
								}
								index = Array.FindIndex(array, reverseOrderByteBuffer.CurrentIndex, num2 - reverseOrderByteBuffer.CurrentIndex, (byte b) => b != 0);
							}
							if (index < 0)
							{
								offset = 0;
							}
						}
						_sleepCounter = 0;
					}
					else
					{
						_sleepCounter++;
						if (_sleepCounter == 1000.0)
						{
							Thread.Sleep(1);
							_sleepCounter = 0;
						}
					}
				}
				catch (Exception)
				{
					_reading = false;
					Thread.Sleep(1);
				}
			}
		}
	}
}
