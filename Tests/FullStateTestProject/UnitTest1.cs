using NUnit.Framework;
using System.Collections.Generic;
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

        Dictionary<int, int> sessions = new();
        Dictionary<int, string> cookies = new();
        Dictionary<int, Dictionary<int, int>> another1 = new();
        Dictionary<int, Dictionary<int, int>> another2 = new();
        Dictionary<int, int> infoProvider1 = new();
        Dictionary<int, int> infoProvider3 = new();
        Dictionary<int, Dictionary<int, int>> infoProvider2 = new();
        Dictionary<int, Dictionary<int, int>> infoProvider4 = new();
        Dictionary<int, Dictionary<int, int>> another32 = new();
        Dictionary<int, Dictionary<int, int>> another42 = new();
        Dictionary<int, int> another31 = new();
        Dictionary<int, Dictionary<int, int>> another41 = new();
        Dictionary<int, int> infoProviderNum = new();

        while (server.StatHolder.Asserts.TryDequeue(out AssertHolder assertHolder))
        {
            Trace.WriteLine(assertHolder);

            if (assertHolder.Selector == "session")
            {
                if (assertHolder.Request == 0 && !sessions.ContainsKey(assertHolder.Client))
                {
                    sessions[assertHolder.Client] = assertHolder.Value;
                }
                else
                {
                    Assert.That(sessions[assertHolder.Client] == assertHolder.Value);
                }

            }
            else if (assertHolder.Selector == "infoProvider1")
            {
                if (assertHolder.Request == 0 && !infoProvider1.ContainsKey(assertHolder.Client))
                {
                    infoProvider1[assertHolder.Client] = assertHolder.Value;
                }
                else
                {
                    Assert.That(infoProvider1[assertHolder.Client] == assertHolder.Value);
                }
                infoProviderNum[assertHolder.Client] = 1;
            }
            else if (assertHolder.Selector == "infoProvider3")
            {
                if (assertHolder.Request == 0 && !infoProvider3.ContainsKey(assertHolder.Client))
                {
                    infoProvider3[assertHolder.Client] = assertHolder.Value;
                }
                else
                {
                    Assert.That(infoProvider3[assertHolder.Client] == assertHolder.Value);
                }

            }
            else if (assertHolder.Selector == "another1")
            {
                if (!another1.ContainsKey(assertHolder.Client))
                {
                    another1[assertHolder.Client] = new Dictionary<int, int>();
                }
                if (!another1[assertHolder.Client].ContainsKey(assertHolder.Request))
                {
                    another1[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                }
                else
                {
                    Assert.Fail();
                }
            }
            else if (assertHolder.Selector == "another2")
            {
                if (!another2.ContainsKey(assertHolder.Client))
                {
                    another2[assertHolder.Client] = new Dictionary<int, int>();
                }
                if (!another2[assertHolder.Client].ContainsKey(assertHolder.Request))
                {
                    another2[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                }
                else
                {
                    Assert.Fail();
                }
            }
            else if (assertHolder.Selector == "infoProvider2")
            {
                if (!infoProvider2.ContainsKey(assertHolder.Client))
                {
                    infoProvider2[assertHolder.Client] = new Dictionary<int, int>();
                }
                if (!infoProvider2[assertHolder.Client].ContainsKey(assertHolder.Request))
                {
                    infoProvider2[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                }
                else
                {
                    Assert.Fail();
                }
                infoProviderNum[assertHolder.Client] = 2;
            }
            else if (assertHolder.Selector == "infoProvider4")
            {
                if (!infoProvider4.ContainsKey(assertHolder.Client))
                {
                    infoProvider4[assertHolder.Client] = new Dictionary<int, int>();
                }
                if (!infoProvider4[assertHolder.Client].ContainsKey(assertHolder.Request))
                {
                    infoProvider4[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                }
                else
                {
                    Assert.That(infoProvider4[assertHolder.Client][assertHolder.Request] == assertHolder.Value);
                }
            }
            else if (assertHolder.Selector == "another3")
            {
                if (infoProviderNum.ContainsKey(assertHolder.Client))
                {
                    if (infoProviderNum[assertHolder.Client] == 2)
                    {
                        if (!another32.ContainsKey(assertHolder.Client))
                        {
                            another32[assertHolder.Client] = new Dictionary<int, int>();
                        }
                        if (!another32[assertHolder.Client].ContainsKey(assertHolder.Request))
                        {
                            another32[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                        }
                        else
                        {
                            Assert.Fail();
                        }
                    }
                    else if(infoProviderNum[assertHolder.Client] == 1)
                    {
                        if (assertHolder.Request == 0 && !another31.ContainsKey(assertHolder.Client))
                        {
                            another31[assertHolder.Client] = assertHolder.Value;
                        }
                        else
                        {
                            Assert.That(another31[assertHolder.Client] == assertHolder.Value);
                        }
                    }
                }
            }
            else if (assertHolder.Selector == "another4")
            {
                if (infoProviderNum.ContainsKey(assertHolder.Client))
                {
                    if (infoProviderNum[assertHolder.Client] == 2)
                    {
                        if (!another42.ContainsKey(assertHolder.Client))
                        {
                            another42[assertHolder.Client] = new Dictionary<int, int>();
                        }
                        if (!another42[assertHolder.Client].ContainsKey(assertHolder.Request))
                        {
                            another42[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                        }
                        else
                        {
                            Assert.Fail();
                        }
                    }
                    else if (infoProviderNum[assertHolder.Client] == 1)
                    {
                        if (!another41.ContainsKey(assertHolder.Client))
                        {
                            another41[assertHolder.Client] = new Dictionary<int, int>();
                        }
                        if (!another41[assertHolder.Client].ContainsKey(assertHolder.Request))
                        {
                            another41[assertHolder.Client][assertHolder.Request] = assertHolder.Value;
                        }
                        else
                        {
                            Assert.Fail();
                        }
                    }
                }
            }

            if (assertHolder.Request == 1)
            {
                if (!cookies.ContainsKey(assertHolder.Client))
                {
                    cookies[assertHolder.Client] = assertHolder.Session;
                }
                else
                {
                    Assert.That(cookies[assertHolder.Client] == assertHolder.Session);
                }
            }


        }

        Assert.That(sessions[0] != sessions[1]);

        Assert.That(cookies[0] != cookies[1]);

        Assert.That(another1[0][0] == another2[0][0]);
        Assert.That(another1[0][1] == another2[0][1]);
        Assert.That(another1[1][0] == another2[1][0]);
        Assert.That(another1[1][1] == another2[1][1]);
        Assert.That(another1[0][0] != another1[0][1]);
        Assert.That(another2[0][0] != another2[0][1]);
        Assert.That(another1[1][0] != another1[1][1]);
        Assert.That(another2[1][0] != another2[1][1]);

        Assert.That(infoProvider1[0] == infoProvider3[0]);
        Assert.That(infoProvider1[1] == infoProvider3[1]);

        Assert.That(infoProvider1[0] != infoProvider1[1]);

        Assert.That(infoProvider3[0] != infoProvider3[1]);

        Assert.That(infoProvider2[0][0] == infoProvider4[0][0]);
        Assert.That(infoProvider2[0][1] == infoProvider4[0][1]);
        Assert.That(infoProvider2[1][0] == infoProvider4[1][0]);
        Assert.That(infoProvider2[1][1] == infoProvider4[1][1]);
        Assert.That(infoProvider2[0][0] != infoProvider2[0][1]);
        Assert.That(infoProvider4[0][0] != infoProvider4[0][1]);
        Assert.That(infoProvider2[1][0] != infoProvider2[1][1]);
        Assert.That(infoProvider4[1][0] != infoProvider4[1][1]);

        Assert.That(another32[0][0] == another42[0][0]);
        Assert.That(another32[0][1] == another42[0][1]);
        Assert.That(another32[1][0] == another42[1][0]);
        Assert.That(another32[1][1] == another42[1][1]);
        Assert.That(another32[0][0] != another32[0][1]);
        Assert.That(another42[0][0] != another42[0][1]);
        Assert.That(another32[1][0] != another32[1][1]);
        Assert.That(another42[1][0] != another42[1][1]);

        Assert.That(another41[0][0] == another1[0][0]);
        Assert.That(another41[0][1] == another1[0][1]);
        Assert.That(another41[1][0] == another1[1][0]);
        Assert.That(another41[1][1] == another1[1][1]);

    }

}
