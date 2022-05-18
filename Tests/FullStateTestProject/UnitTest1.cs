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

        Task[] clients = new Task[2];
        for(int i = 0; i < clients.Length; i++)
        {
            int clientNum = i;
            clients[i] = Task.Run(async () =>
            {
                HttpClient client = new HttpClient();

                client.BaseAddress = server.Uri;

                for(int j = 0; j < 2; j++)
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"/{clientNum}/{j}");

                    HttpResponseMessage response = await client.SendAsync(request);

                    Trace.WriteLine(response.StatusCode);
                    Trace.WriteLine(await response.Content.ReadAsStringAsync());
                }

            });
        }

        await Task.WhenAll(clients);

        await server.StopAsync();
        await serverTask;

        while(server.StatHolder.Asserts.TryDequeue(out AssertHolder assertHolder))
        {
            Trace.WriteLine(assertHolder);
        }

    }

}
