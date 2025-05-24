// TestSignal, Version=24.2.0.0, Culture=neutral, PublicKeyToken=null
// TestSignal.NotifyEventArgs<T>
using System;

public class NotifyEventArgs<T> : EventArgs
{
	private readonly T value;

	public T Value
	{
		get
		{
			return value;
		}
	}

	public NotifyEventArgs(T value)
	{
		this.value = value;
	}
}
