using System.Text;
namespace MotionMonitor
{
	public class WriteDataBuffer
	{
		private readonly MemoryStream _memoryStream;
		public WriteDataBuffer()
		{
			_memoryStream = new MemoryStream(256);
		}

		public byte[] Data
		{
			get
			{
                byte[] array = new byte[_memoryStream.Length];
                Array.ConstrainedCopy(_memoryStream.GetBuffer(), 0, array, 0, (int)_memoryStream.Length);
                return array;
            }
		}
		public byte[] GetData()
		{
			byte[] array = new byte[_memoryStream.Length];
			Array.ConstrainedCopy(_memoryStream.GetBuffer(), 0, array, 0, (int)_memoryStream.Length);
			return array;
		}
		public void AddData(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			Array.Reverse(bytes);
			_memoryStream.Write(bytes, 0, bytes.Length);
		}
		public void AddData(int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			Array.Reverse(bytes);
			_memoryStream.Write(bytes, 0, bytes.Length);
		}
		public void AddData(string value, int size)
		{
			byte[] array = new byte[size];
			byte[] bytes = Encoding.ASCII.GetBytes(value);
			for (int i = 0; i < size && i < bytes.Length; i++)
			{
				array[i] = bytes[i];
			}
			_memoryStream.Write(array, 0, size);
		}
	}
}

