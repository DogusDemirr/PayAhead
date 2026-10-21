using System.Net.Http.Headers;
using System.Text.Json;
using PayAhead.Application.Premium;

namespace PayAhead.Infrastructure;

public sealed class StoreHttp(IHttpClientFactory clients)
{
    public async Task<JsonDocument> JsonAsync(HttpRequestMessage request,CancellationToken ct)
    {
        using var response=await clients.CreateClient("Stores").SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
        if((int)response.StatusCode>=500||response.StatusCode==System.Net.HttpStatusCode.TooManyRequests)throw new StoreUnavailableException();
        if(!response.IsSuccessStatusCode)throw new StoreVerificationException();
        await using var stream=await response.Content.ReadAsStreamAsync(ct);using var output=new MemoryStream();var chunk=new byte[4096];int n;
        while((n=await stream.ReadAsync(chunk.AsMemory(),ct))>0) { if(output.Length+n>512*1024)throw new StoreVerificationException();await output.WriteAsync(chunk.AsMemory(0,n),ct); }
        try { return JsonDocument.Parse(output.Length==0?"{}"u8.ToArray():output.ToArray()); }catch(JsonException){throw new StoreVerificationException();}
    }
    public static HttpRequestMessage Get(string url,string token)=>new(HttpMethod.Get,url){Headers={Authorization=new AuthenticationHeaderValue("Bearer",token)}};
}
