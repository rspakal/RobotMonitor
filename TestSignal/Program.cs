using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.CompilerServices;
using TestSignal;
using TestSignalLogger;
double sampleTime = 0;
bool antiAliasFiltering = false;
bool zeroOrderHold = false;
int maxLogTime = 0;

Signal[] signals = Signal.BuildSubscribtionsData();
IPAddress ipAddress = new (new byte[4] { 192, 168, 125, 1 });
TestSignalLogData testSignalLogData = new ();
testSignalLogData.Connect(ipAddress);

if (testSignalLogData.testSignalHandler != null && testSignalLogData.testSignalHandler.ConnectionState == ConnectionState.Idle)
{
    List<TestSignalProvider> testSignalProviders = testSignalLogData.testSignalProviders;
    if (testSignalProviders != null)
    {
        testSignalProviders.Clear();
    }
    double axcSampleTime = Signal.AxcSampleTime;
    if (sampleTime <= 0.0)
    {
        sampleTime = axcSampleTime;
    }
    double num2 = sampleTime / axcSampleTime;
    sampleTime = num2 * (axcSampleTime + 5E-07);
    sampleTime *= 1000.0;
    testSignalLogData.testSignalHandler.AntiAliasFiltering = antiAliasFiltering;
    testSignalLogData.testSignalHandler.SampleTime = sampleTime;
    testSignalLogData.testSignalHandler.ZeroOrderHold = zeroOrderHold;
    testSignalLogData.testSignalHandler.MaxLogTime = maxLogTime;
    testSignalLogData.testSignalProviders ??= new List<TestSignalProvider>();
    Signal[] array = signals;
    testSignalLogData.testSignalProviders.AddRange(
        signals.Where(s => s is not null).Select(s => new TestSignalProvider(s.MechUnit, s.TestSignal, s.Axis + 1, sampleTime, antiAliasFiltering, s.Trig)));
    IMeasurementProviderHandler testSignalHandler = testSignalLogData.testSignalHandler;
    IMeasurementsProvider[] providers = testSignalLogData.testSignalProviders.ToArray();
    if (!testSignalHandler.Init(providers))
    {
        await testSignalLogData.StopLog();
    }
    List<TestSignalProvider>.Enumerator enumerator = testSignalLogData.testSignalProviders.GetEnumerator();
}
Console.ReadLine();


