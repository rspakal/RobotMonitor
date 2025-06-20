namespace TestSignal
{
	public class PropertyChangedEventArgs<T> : EventArgs
	{
		private readonly T _newValue;
		private readonly T _oldValue;
		public T NewValue => _newValue;
		public T OldValue => _oldValue;
		public PropertyChangedEventArgs(T oldValue, T newValue)
		{
			_newValue = newValue;
			_oldValue = oldValue;
		}
	}
}
