namespace TestSignalLogger
{
	[Serializable]
	public class Trig
	{
		private bool Initialized;
		private bool Active;
		public TrigType Type { get; set; }
		public double Limit { get; set; }
		public double PostTime { get; set; }
		public bool RestartLog { get; set; }
		public bool OnFallingEdge { get; set; }
		private static TrigType Invert(TrigType type)
		{
			switch (type)
			{
				case TrigType.GreaterThan:
					return TrigType.LessThanOrEqual;
				case TrigType.AbsGreaterThan:
					return TrigType.AbsLessThanOrEqual;
				case TrigType.GreaterThanOrEqual:
					return TrigType.LessThan;
				case TrigType.AbsGreaterThanOrEqual:
					return TrigType.AbsLessThan;
				case TrigType.LessThan:
					return TrigType.GreaterThanOrEqual;
				case TrigType.AbsLessThan:
					return TrigType.AbsGreaterThanOrEqual;
				case TrigType.LessThanOrEqual:
					return TrigType.GreaterThan;
				case TrigType.AbsLessThanOrEqual:
					return TrigType.AbsGreaterThan;
				default:
					return TrigType.No;
			}
		}

		public void Clear()
		{
			Active = false;
			Initialized = false;
		}

		public bool Activated(double value)
		{
			bool flag = false;
			TrigType trigType = Type;
			if (Active && OnFallingEdge)
			{
				trigType = Invert(trigType);
			}
			switch (trigType)
			{
				case TrigType.GreaterThan:
					flag = value > Limit;
					break;
				case TrigType.AbsGreaterThan:
					flag = Math.Abs(value) > Limit;
					break;
				case TrigType.GreaterThanOrEqual:
					flag = value >= Limit;
					break;
				case TrigType.AbsGreaterThanOrEqual:
					flag = Math.Abs(value) >= Limit;
					break;
				case TrigType.LessThan:
					flag = value < Limit;
					break;
				case TrigType.AbsLessThan:
					flag = Math.Abs(value) < Limit;
					break;
				case TrigType.LessThanOrEqual:
					flag = value <= Limit;
					break;
				case TrigType.AbsLessThanOrEqual:
					flag = Math.Abs(value) <= Limit;
					break;
			}
			if (OnFallingEdge)
			{
				if (!Active && flag)
				{
					flag = false;
					Active = true;
				}
				else if (Active && flag)
				{
					Active = false;
				}
			}
			else
			{
				Active = flag;
			}
			flag &= Initialized;
			Initialized = true;
			return flag;
		}
	}
}
