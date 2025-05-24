namespace TestSignal
{
	public class PropertyChangedEventArgs<T> : EventArgs
	{
		private readonly T newValue;

		private readonly T oldValue;

		public T NewValue
		{
			get
			{
				return newValue;
			}
		}

		public T OldValue
		{
			get
			{
				return oldValue;
			}
		}

		public PropertyChangedEventArgs(T oldValue, T newValue)
		{
			this.newValue = newValue;
			this.oldValue = oldValue;
		}
	}
}
