public class NotifyEventArgs<T> : EventArgs
{
	private readonly T _value;
	public T Value => _value;
	public NotifyEventArgs(T value) => _value = value;
}
