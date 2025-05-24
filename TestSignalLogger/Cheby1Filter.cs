using System;
using TestSignalLogger;
namespace TestSignalLogger
{
	[Serializable]
	public class Cheby1Filter : Filter
	{
		private static readonly double[] ChebyshevA = new double[9] { 1.0, -6.915597215654454, 21.109544875194082, -37.13027866483087, 41.14587013716249, -29.405593178649198, 13.231765761824228, -3.426636689952911, 0.390938300717147 };

		private static readonly double[] ChebyshevB = new double[9] { 5.204795471035E-08, 4.1638363768279E-07, 1.45734273188978E-06, 2.91468546377955E-06, 3.64335682972444E-06, 2.91468546377955E-06, 1.45734273188978E-06, 4.1638363768279E-07, 5.204795471035E-08 };

		private const int delay = 8;

		public static int Delay
		{
			get
			{
				return 8;
			}
		}

		public Cheby1Filter(int sampleFactor)
			: base(sampleFactor, ChebyshevA, ChebyshevB)
		{
		}
	}
}
