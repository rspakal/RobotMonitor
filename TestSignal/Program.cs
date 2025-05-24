using System.Net;
using TestSignal;
var ipAddress = new IPAddress(new byte[4] { 127, 0, 0, 1 });
var testSignalHandler = new TestSignalHandler(ipAddress);
var testSignalLogData =  new TestSignalLogData();
testSignalLogData.Connect(ipAddress.ToString());