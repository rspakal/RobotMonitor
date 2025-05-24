using System.Text;
namespace TestSignal
{
	public class ReverseByteOrderWriter
	{
		private readonly MemoryStream ms;

		public ReverseByteOrderWriter()
		{
			ms = new MemoryStream(256);
		}

		public byte[] GetBytes()
		{
			byte[] array = new byte[ms.Length];
			Array.ConstrainedCopy(ms.GetBuffer(), 0, array, 0, (int)ms.Length);
			return array;
		}

		public void Write(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			Array.Reverse(bytes);
			ms.Write(bytes, 0, bytes.Length);
		}

		public void Write(int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			Array.Reverse(bytes);
			ms.Write(bytes, 0, bytes.Length);
		}

		public void Write(string value, int size)
		{
			byte[] array = new byte[size];
			byte[] bytes = Encoding.ASCII.GetBytes(value);
			for (int i = 0; i < size && i < bytes.Length; i++)
			{
				array[i] = bytes[i];
			}
			ms.Write(array, 0, size);
		}
	}
}

