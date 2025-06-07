namespace TestSignalLogger
{
    [Serializable]
	public class Filter
	{
		private readonly double[] a;
		private readonly double[] b;
		private readonly int M;
		private readonly int sampleFactor;
		private int sampleCounter;
		private readonly List<double> x;
		private readonly List<double> y;

		public bool SampleFinished => sampleFactor == sampleCounter;
		public double Value => y[y.Count - 1];

		public Filter(int sampleFactor, double[] a, double[] b)
		{
			this.sampleFactor = sampleFactor;
			this.a = a;
			this.b = b;
			M = b.Length;
			x = new List<double>(M);
			for (int i = 0; i < M; i++)
			{
				x.Add(0.0);
			}
			y = new List<double>(M);
			for (int j = 0; j < M; j++)
			{
				y.Add(0.0);
			}
		}

		public void Execute(double x)
		{
			if (sampleCounter == sampleFactor)
			{
				sampleCounter = 1;
			}
			else
			{
				sampleCounter++;
			}
			this.x.RemoveAt(0);
			this.x.Add(x);
			y.RemoveAt(0);
			y.Add(0.0);
			double num = 0.0;
			for (int i = 0; i < M; i++)
			{
				num += b[i] * this.x[M - 1 - i];
			}
			for (int j = 1; j < M; j++)
			{
				num -= a[j] * y[M - 1 - j];
			}
			y[y.Count - 1] = num;
		}
	}
}
