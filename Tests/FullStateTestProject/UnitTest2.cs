using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System.Diagnostics;
using System.Linq;

namespace FullStateTestProject;

public class UnitTest2
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ConsoleTraceListener());
        Trace.AutoFlush = true;
    }

}
