using MotionMonitor.Enums;
using System.Net.Sockets;
namespace TestSignal
{
    public abstract class LogSrvStreamHandler
	{

		private const int READ_BUFFER_LENGTH = 7712;
		private const int LOGSRV_MAX_ERROR_MESSAGE_SIZE = 80;
		public const int MAX_SIGNALS_AMOUNT = 12;
		private const int LOGSRV_MECHANICAL_UNIT_MAX_NAME_LENGTH = 40;
		private bool _reading;
		private NetworkStream? _networkStream;
		private Thread? _readThread;
		private int _sleepCounter;
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

		protected virtual void OnLogData(int channel, int count, ReadDataBuffer robb)
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

		protected void WriteRemoveAllSignals(LogSrvCommand cmd)
		{
			WriteDataBuffer dataBuffer = new();
			dataBuffer.AddData((int)cmd);
			Write(dataBuffer.GetData());
		}

		public void WriteStartStopLog(LogSrvCommand cmd, int[] channels)
		{
            WriteDataBuffer dataBuffer = new();
            dataBuffer.AddData((int)cmd);
            dataBuffer.AddData(channels.Length);
			for (int i = 0; i < 12; i++)
			{
				var data = i < channels.Length ? channels[i] : -1;
				dataBuffer.AddData(data);
            }

			Write(dataBuffer.GetData());

			//?????
            OnLogStartedStopped(cmd);
        }

		protected void WriteDefineSignal(int channelNo, int signalNo, string mechUnitName, int axisNo, float sampleTime)
		{
			if (channelNo < 0 || channelNo > 11)
			{
				throw new ArgumentOutOfRangeException("channel", channelNo, string.Format("Value of must be between 0 and {0}", 11));
			}
			if (axisNo < 1 || axisNo > 6)
			{
				throw new ArgumentOutOfRangeException("axisNumber", axisNo, "Value of must be between 1 and 6");
			}
			WriteDataBuffer dataBuffer = new ();
			dataBuffer.AddData(1);
			dataBuffer.AddData(channelNo);
			dataBuffer.AddData(signalNo);
			dataBuffer.AddData(mechUnitName, 40);
			dataBuffer.AddData(axisNo - 1);
			dataBuffer.AddData(sampleTime);
			Write(dataBuffer.GetData());
		}

		protected void WriteRemoveSignal(LogSrvCommand cmd, int channel)
		{
			WriteDataBuffer dataBuffer = new ();
			dataBuffer.AddData((int)cmd);
			dataBuffer.AddData(channel);
			Write(dataBuffer.GetData());
		}

		protected void WriteEnumerateSignals(LogSrvCommand cmd)
		{
			WriteDataBuffer dataBuffer = new ();
			dataBuffer.AddData((int)cmd);
			Write(dataBuffer.GetData());
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
			byte[] array = new byte[READ_BUFFER_LENGTH];
			ReadDataBuffer dataBuffer = new (array);
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
						bytesRead = _networkStream.Read(array, offset, READ_BUFFER_LENGTH - offset);
						num2 = offset + bytesRead;
						if (bytesRead > 0)
						{
							//Edited
							//num4 = Array.FindIndex(array, 0, num2 - offset, (byte b) => b != 0);
							index = Array.FindIndex(array, 0, bytesRead, b => b != 0);
							//------
							while (index >= 0 && !flag)
							{
								dataBuffer.CurrentIndex = index;
								dataBuffer.Skip(1);
								switch (array[index])
								{
									case 7:
										{
											if (index + 3856 - 3 > num2)
											{
												flag = true;
												break;
											}
											int count = dataBuffer.ReadInt();
											int channel = dataBuffer.ReadInt();
											dataBuffer.ReadInt();
											OnLogData(channel, count, dataBuffer);
											break;
										}
									case 55:
										if (index + 4 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnAllSignalsRemoved(dataBuffer.ReadInt());
										}
										break;
									case 60:
										if (index + 80 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnError(dataBuffer.ReadString(80));
										}
										break;
									case 51:
										if (index + 4 + 60 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnSignalDefined(dataBuffer.ReadInt(), new LogSrvSignalDefinition(dataBuffer));
										}
										break;
									case 54:
										if (index + 4 - 3 > num2)
										{
											flag = true;
										}
										else
										{
											OnSignalRemoved(dataBuffer.ReadInt());
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
												array2[num6] = new LogSrvSignalDefinition(dataBuffer);
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
								index = Array.FindIndex(array, dataBuffer.CurrentIndex, num2 - dataBuffer.CurrentIndex, (byte b) => b != 0);
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
						if (_sleepCounter == 1000)
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
