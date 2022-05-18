using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FullStateTestProject;

public class Tests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
        Trace.AutoFlush = true;
    }

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [Test]
    public async Task TestSessions()
    {
        Server server = new();
        Task serverTask = Task.Run(() => server.Run());

        Trace.WriteLine("waiting");
        server.IsRunning.Wait();

        Trace.WriteLine("done");
        HttpClient client = new HttpClient();

        client.BaseAddress = server.Uri;

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/1");

        HttpResponseMessage response = await client.SendAsync(request);

        Trace.WriteLine(await response.Content.ReadAsStringAsync());

        await server.StopAsync();
        await serverTask;

        await Task.CompletedTask;
    }

}
