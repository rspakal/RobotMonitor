using System.Text;
namespace TestSignal
{
	public class ReadDataBuffer
	{
		private const byte INT32_LENGHT = 4;
		private readonly byte[] _data;
		private int _currentIndex;
		public int CurrentIndex
		{
			get => _currentIndex;
			set => _currentIndex = value;
		}

		public byte[] RemainingBuffer
		{
			get
			{
				byte[] array = new byte[_data.Length - _currentIndex];
				Array.Copy(_data, _currentIndex, array, 0, array.Length);
				return array;
			}
		}

		public ReadDataBuffer(byte[] data, int start = 0)
		{
			_data = data;
			_currentIndex = start;
		}

		public void Skip(int count)
		{
			_currentIndex += count;
		}

		public int ReadInt()
		{
			Array.Reverse(_data, _currentIndex, INT32_LENGHT);
			int result = BitConverter.ToInt32(_data, _currentIndex);
			_currentIndex += INT32_LENGHT;
			return result;
		}

		public double ReadFloat()
		{
			Array.Reverse(_data, _currentIndex, INT32_LENGHT);
			double result = BitConverter.ToSingle(_data, _currentIndex);
			_currentIndex += INT32_LENGHT;
			return result;
		}

		public string ReadString(int size)
		{
			int num = Math.Min(_data.Length - _currentIndex, size);
			int num2 = Array.FindIndex(_data, _currentIndex, num, (byte dataValue) => dataValue == 0);
			string result = Encoding.ASCII.GetString(_data, _currentIndex, (num2 < 0) ? num : (num2 - _currentIndex));
			_currentIndex += size;
			return result;
		}
	}
}