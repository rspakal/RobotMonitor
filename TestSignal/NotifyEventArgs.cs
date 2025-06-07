// TestSignal, Version=24.2.0.0, Culture=neutral, PublicKeyToken=null
// TestSignal.NotifyEventArgs<T>
using System;

public class NotifyEventArgs<T> : EventArgs
{
	private readonly T _value;
	public T Value => _value;
	public NotifyEventArgs(T value) => _value = value;
}
