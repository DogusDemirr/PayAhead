namespace PayAhead.Api;

public static class ContainerProbe
{
    public static async Task<int> RunAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            return (await http.GetAsync("http://127.0.0.1:8080/health/live")).IsSuccessStatusCode ? 0 : 1;
        }
        catch { return 1; }
    }
}
