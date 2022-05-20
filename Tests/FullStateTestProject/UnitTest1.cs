using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FullStateTestProject;

public class Tests
{

    private const string Url = "http://localhost:5555";
    private const string FullStateTestServer = nameof(FullStateTestServer);
    private const string FullStateTestProject = nameof(FullStateTestProject);
    private static readonly Regex testTransientProbe = new Regex(@"/ITransient[0-2]$");
    private static readonly Regex testScopedProbe = new Regex(@"/IScoped[0-2]$");
    private static readonly Regex testSingletonProbe = new Regex(@"/ISingleton[0-2]$");


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

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public async Task TestSessions()
    {
        TimeSpan connectionTimeout = TimeSpan.FromSeconds(10);
        int connectionTryInterval = 1000;
        int numberOfClients = 3;
        int numberOfREquests = 3;
        int depth = 3;

        Process[] processes = Process.GetProcessesByName(FullStateTestServer);
        foreach (Process process in processes)
        {
            process.Kill();
        }
        string wd = Directory.GetCurrentDirectory().Replace(FullStateTestProject, FullStateTestServer);

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = FullStateTestServer,
            Arguments = $"applicationUrl={Url} depth={depth}",
            WorkingDirectory = wd
        };
        Process serverProcess = Process.Start(processStartInfo);

        {
            // ���, ����� ���������� ������
            HttpClient client = new HttpClient();
            DateTime start = DateTime.Now;

            while (DateTime.Now - start < connectionTimeout)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/");
                    HttpResponseMessage response = await client.SendAsync(request);
                    break;
                }
                catch
                {
                    await Task.Delay(connectionTryInterval);
                }
            }
        }

        ConcurrentQueue<AssertHolder> asserts = new();

        Task[] clients = new Task[numberOfClients];
        for (int i = 0; i < clients.Length; i++)
        {
            int clientNum = i;
            clients[i] = Task.Run(async () =>
            {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(Url);

                for (int j = 0; j < numberOfREquests; j++)
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"/{clientNum}/{j}");

                    HttpResponseMessage response = await client.SendAsync(request);

                    Assert.That(response.StatusCode == System.Net.HttpStatusCode.OK);

                    string data = await response.Content.ReadAsStringAsync();
                    foreach (AssertHolder assert in JsonSerializer.Deserialize<List<AssertHolder>>(data)!)
                    {
                        asserts.Enqueue(assert);
                    }
                }

            });
        }

        await Task.WhenAll(clients);

        serverProcess.Kill();

        HashSet<int> singletons = new();
        HashSet<int> transients = new();

        Dictionary<int, string> sessions = new();

        Dictionary<int, int> sessionScopeds = new();
        Dictionary<int, int> requestScopeds = new();

        HashSet<int> rootScopeds = new();

        int count = 0;

        while (asserts.TryDequeue(out AssertHolder? assert))
        {

            count++;
            //Trace.WriteLine(assert);

            int request = assert.Client * numberOfREquests + assert.Request;

            Assert.That(assert.Error, Is.Null);

            if (assert.Request == 0)
            {
                Assert.That(assert.Session, Is.Null);
            }
            else
            {
                Assert.That(assert.Session, Is.Not.Null);
                if (assert.Request == 1)
                {
                    sessions[assert.Client] = assert!.Session;
                }
                else
                {
                    Assert.That(sessions[assert.Client], Is.EqualTo(assert.Session));
                }
            }


            if (testSingletonProbe.IsMatch(assert.Selector))
            {
                // ��� ��������� �����
                if (singletons.Count == 0)
                {
                    singletons.Add(assert.ObjectId);
                }
                else
                {
                    Assert.That(singletons.Add(assert.ObjectId), Is.Not.True);
                }
            }
            else if (testTransientProbe.IsMatch(assert.Selector))
            {
                // ��� ���������� ������
                Assert.That(transients.Add(assert.ObjectId), Is.True);
            }
            else if (testScopedProbe.IsMatch(assert.Selector))
            {
                int[] scopePath = assert.Selector.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.StartsWith("ISingleton") ? -1 : int.Parse(s.Substring(s.Length - 1))).ToArray();


                int effectiveScope = 1;
                int i = scopePath.Length - 1;
                for (; i >= 0; --i)
                {
                    if (scopePath[i] != 0)
                    {
                        effectiveScope = scopePath[i];
                        break;
                    }
                }

                if (effectiveScope == 1)
                {
                    // request scope

                    if (!requestScopeds.ContainsKey(request))
                    {
                        requestScopeds[request] = assert.ObjectId;
                    }
                    else
                    {
                        Assert.That(
                            requestScopeds[request], Is.EqualTo(assert.ObjectId),
                            string.Join(
                                "\n", asserts.Where(
                                    a =>
                                    a.Client == assert.Client && a.Request == assert.Request
                                    && testScopedProbe.IsMatch(a.Selector)
                                    && (a.Selector.EndsWith("0") || a.Selector.EndsWith("1"))
                                )
                            )
                        );
                    }
                }
                else if (effectiveScope == 2)
                {
                    // session scope
                    if (!sessionScopeds.ContainsKey(assert.Client))
                    {
                        sessionScopeds[assert.Client] = assert.ObjectId;
                    }
                    else
                    {
                        Assert.That(
                            sessionScopeds[assert.Client], Is.EqualTo(assert.ObjectId),
                            string.Join(
                                "\n", asserts.Where(
                                    a =>
                                    a.Client == assert.Client
                                    && testScopedProbe.IsMatch(a.Selector)
                                    && (a.Selector.EndsWith("0") || a.Selector.EndsWith("2"))
                                )
                            )
                        );
                    }
                }
                else if (effectiveScope == -1)
                {
                    //Trace.WriteLine($"{assert}: {effectiveScope}");
                    // root scope
                    // ��� scoped �� root scope �����
                    if (rootScopeds.Count == 0)
                    {
                        rootScopeds.Add(assert.ObjectId);
                    }
                    else
                    {
                        Assert.That(rootScopeds.Add(assert.ObjectId), Is.Not.True);
                    }
                }
                else
                {
                    Assert.Fail($"{assert.ToString()}: {effectiveScope}");
                }
            }
            else
            {
                Assert.Fail(assert.ToString());
            }
        }

        Assert.That(count, 
            Is.EqualTo((int)Math.Round(numberOfClients * numberOfREquests * (Math.Pow(9, depth + 1) - 9) / 8)),
            count.ToString());


    }

}
