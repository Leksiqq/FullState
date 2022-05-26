using FullStateTestServer;

HttpClient client = new HttpClient();

client.BaseAddress = new Uri("http://localhost:5195");

Task[] parralelRequests = new Task[4];

for(int i = 0; i < parralelRequests.Length; ++i)
{
    parralelRequests[i] = Task.Run(async () => 
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/");

        HttpResponseMessage response = await client.SendAsync(request);

        string data = await response.Content.ReadAsStringAsync();

        Console.WriteLine(data);
    });
    if(i == 0)
    {
        await parralelRequests[0];
    }
}


await Task.WhenAll(parralelRequests);
