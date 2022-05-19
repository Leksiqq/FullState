using FullStateTestServer;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
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
    public async Task TestSessions()
    {
        ConcurrentQueue<AssertHolder> asserts = new();

        Task[] clients = new Task[3];
        for (int i = 0; i < clients.Length; i++)
        {
            int clientNum = i;
            clients[i] = Task.Run(async () =>
            {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri("http://localhost:5211");

                for (int j = 0; j < 3; j++)
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"/{clientNum}/{j}");

                    HttpResponseMessage response = await client.SendAsync(request);

                    Trace.WriteLine(response.StatusCode);
                    Trace.WriteLine(await response.Content.ReadAsStringAsync());
                }

            });
        }

        await Task.WhenAll(clients);

        Regex testTransientProbe = new Regex(@"/TransientProbe[0-2]$");
        Regex testScopedProbe = new Regex(@"/ScopedProbe[0-2]$");
        Regex testSingletonProbe = new Regex(@"/SingletonProbe[0-2]$");

        HashSet<int> singletons = new();
        HashSet<int> transients = new();

        while (asserts.TryDequeue(out AssertHolder assert))
        {
            Trace.WriteLine(assert);

            if(testSingletonProbe.IsMatch(assert.Selector))
            {
                if(singletons.Count == 0)
                {
                    singletons.Add(assert.Value);
                }
                else
                {
                    Assert.That(!singletons.Add(assert.Value));
                }
            }
        }

    }

}
