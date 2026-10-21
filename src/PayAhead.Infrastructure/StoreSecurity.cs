using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PayAhead.Application.Premium;

namespace PayAhead.Infrastructure;

public sealed class StoreOptions
{
    public bool AppleEnabled { get; init; }
    public bool GoogleEnabled { get; init; }
    public bool WorkerEnabled { get; init; }
    public string AppleEnvironment { get; init; } = "Production";
    public string AppleBundleId { get; init; } = "";
    public long AppleAppId { get; init; }
    public string AppleIssuerId { get; init; } = "";
    public string AppleKeyId { get; init; } = "";
    public string ApplePrivateKeyPem { get; init; } = "";
    public string[] AppleRootCertificatesBase64 { get; init; } = [];
    public string[] AppleProducts { get; init; } = [];
    public string GooglePackageName { get; init; } = "";
    public string GoogleEnvironment { get; init; } = "Production";
    public string GoogleServiceAccountEmail { get; init; } = "";
    public string GooglePrivateKeyPem { get; init; } = "";
    public string GooglePushAudience { get; init; } = "";
    public string GooglePushEmail { get; init; } = "";
    public string GooglePushSubscription { get; init; } = "";
    public string[] GoogleProducts { get; init; } = [];
    public string EncryptionKeyId { get; init; } = "v1";
    public Dictionary<string,string> EncryptionKeys { get; init; } = [];
    public static StoreOptions Load(IConfiguration config)
    {
        var value=config.GetSection("Stores").Get<StoreOptions>()??new();
        if(value.AppleEnabled&&(value.AppleEnvironment is not ("Production" or "Sandbox")||value.AppleAppId<=0||string.IsNullOrWhiteSpace(value.AppleBundleId)
            ||value.AppleProducts.Length==0||value.AppleRootCertificatesBase64.Length==0||string.IsNullOrWhiteSpace(value.AppleIssuerId)||string.IsNullOrWhiteSpace(value.AppleKeyId)||string.IsNullOrWhiteSpace(value.ApplePrivateKeyPem)))throw new InvalidOperationException("Apple store configuration is incomplete.");
        if(value.GoogleEnabled&&(value.GoogleEnvironment is not ("Production" or "Sandbox")||string.IsNullOrWhiteSpace(value.GooglePackageName)||value.GoogleProducts.Length==0||string.IsNullOrWhiteSpace(value.GoogleServiceAccountEmail)||string.IsNullOrWhiteSpace(value.GooglePrivateKeyPem)
            ||string.IsNullOrWhiteSpace(value.GooglePushAudience)||string.IsNullOrWhiteSpace(value.GooglePushEmail)||string.IsNullOrWhiteSpace(value.GooglePushSubscription)))throw new InvalidOperationException("Google store configuration is incomplete.");
        if(value.AppleEnabled||value.GoogleEnabled) _=new PurchaseProtector(value).Protect("startup-validation");
        return value;
    }
}
public sealed class PurchaseProtector(StoreOptions options)
{
    public string Protect(string value)
    {
        var key=Key(options.EncryptionKeyId);var nonce=RandomNumberGenerator.GetBytes(12);var tag=new byte[16];var input=Encoding.UTF8.GetBytes(value);var output=new byte[input.Length];
        using var aes=new AesGcm(key,16);aes.Encrypt(nonce,input,output,tag,Encoding.UTF8.GetBytes("PayAhead.Store.v1"));
        return options.EncryptionKeyId+"."+Convert.ToBase64String(nonce.Concat(tag).Concat(output).ToArray());
    }
    public string Unprotect(string value)
    {
        try { var parts=value.Split('.',2);var bytes=Convert.FromBase64String(parts[1]);var output=new byte[bytes.Length-28];using var aes=new AesGcm(Key(parts[0]),16);
            aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),output,Encoding.UTF8.GetBytes("PayAhead.Store.v1"));return Encoding.UTF8.GetString(output); }
        catch(Exception ex)when(ex is CryptographicException or FormatException or IndexOutOfRangeException or OverflowException){throw new StoreUnavailableException();}
    }
    private byte[] Key(string id)
    {
        if(!options.EncryptionKeys.TryGetValue(id,out var text))throw new StoreUnavailableException();
        var key=Convert.FromBase64String(text);if(key.Length!=32||key.Distinct().Count()<16)throw new StoreUnavailableException();return key;
    }
    public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
