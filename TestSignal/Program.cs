using System.Net;
using System.Runtime.CompilerServices;
using TestSignal;
using TestSignalLogger;
double sampleTime = 0;
bool antiAliasFiltering = false;
bool zeroOrderHold = false;
int maxLogTime = 0;
Signal[] signals = { new Signal("T_ROB1", 1, 1717), new Signal("T_ROB1", 2, 1717) };
var ipAddress = new IPAddress(new byte[4] { 127, 0, 0, 1 });
var testSignalLogData = new TestSignalLogData();

testSignalLogData.Connect("192.168.125.1");
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
    //foreach (Signal signal in array)
    //{
    //    testSignalLogData.testSignalProviders.Add(
    //        new TestSignalProvider(
    //            signal.MechUnit, 
    //            signal.TestSignal, 
    //            signal.Axis + 1, 
    //            sampleTime, 
    //            antiAliasFiltering, 
    //            signal.Trig));
    //}

    IMeasurementProviderHandler testSignalHandler = testSignalLogData.testSignalHandler;
    IMeasurementsProvider[] providers = testSignalLogData.testSignalProviders.ToArray();
    if (!testSignalHandler.Init(providers))
    {
        await testSignalLogData.StopLog();
    }
    List<TestSignalProvider>.Enumerator enumerator = testSignalLogData.testSignalProviders.GetEnumerator();
}
Console.ReadLine();


