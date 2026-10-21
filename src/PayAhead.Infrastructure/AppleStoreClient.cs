using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Premium;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class AppleSignedDataVerifier(StoreOptions options)
{
    public JsonDocument Verify(string jws)
    {
        try
        {
            var parts=jws.Split('.');if(parts.Length!=3||jws.Length>100_000)throw new StoreVerificationException();
            using var header=JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parts[0]));var h=header.RootElement;
            if(h.GetProperty("alg").GetString()!="ES256"||!h.TryGetProperty("x5c",out var list)||list.GetArrayLength()!=3)throw new StoreVerificationException();
            var certificates=list.EnumerateArray().Select(x=>new X509Certificate2(Convert.FromBase64String(x.GetString()!))).ToArray();
            try
            {
                using var chain=new X509Chain();chain.ChainPolicy.TrustMode=X509ChainTrustMode.CustomRootTrust;
                foreach(var root in options.AppleRootCertificatesBase64)
                {
                    var bytes=Convert.FromBase64String(root);
                    if(Convert.ToHexString(SHA256.HashData(bytes))!="63343ABFB89A6A03EBB57E9B3F5FA7BE7C4F5C756F3017B3A8C488C3653E9179")throw new StoreVerificationException();
                    chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2(bytes));
                }
                chain.ChainPolicy.ExtraStore.AddRange(certificates.Skip(1).ToArray());chain.ChainPolicy.DisableCertificateDownloads=true;
                chain.ChainPolicy.RevocationMode=X509RevocationMode.NoCheck;
                if(!chain.Build(certificates[0])||chain.ChainElements.Count!=3
                    ||!certificates[0].Extensions.Cast<X509Extension>().Any(x=>x.Oid?.Value=="1.2.840.113635.100.6.11.1")
                    ||!certificates[1].Extensions.Cast<X509Extension>().Any(x=>x.Oid?.Value=="1.2.840.113635.100.6.2.1"))throw new StoreVerificationException();
                // Only trusted Apple certificates may cause online revocation lookups.
                chain.ChainPolicy.RevocationMode=X509RevocationMode.Online;chain.ChainPolicy.RevocationFlag=X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.UrlRetrievalTimeout=TimeSpan.FromSeconds(3);if(!chain.Build(certificates[0]))throw new StoreVerificationException();
                using var key=certificates[0].GetECDsaPublicKey();
                if(key is null||key.KeySize!=256||!key.VerifyData(Encoding.ASCII.GetBytes(parts[0]+"."+parts[1]),Base64UrlEncoder.DecodeBytes(parts[2]),HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation))throw new StoreVerificationException();
                var payload=JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parts[1]));
                if(!payload.RootElement.TryGetProperty("signedDate",out var signed)||signed.GetInt64()>DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds()) { payload.Dispose();throw new StoreVerificationException(); }
                return payload;
            }
            finally { foreach(var certificate in certificates)certificate.Dispose(); }
        }
        catch(Exception ex)when(ex is FormatException or JsonException or CryptographicException or KeyNotFoundException or InvalidOperationException or ArgumentException){throw new StoreVerificationException();}
    }
    public void App(JsonElement value,bool notification=false)
    {
        if(value.GetProperty("bundleId").GetString()!=options.AppleBundleId||value.GetProperty("environment").GetString()!=options.AppleEnvironment
            ||notification&&options.AppleEnvironment=="Production"&&value.GetProperty("appAppleId").GetInt64()!=options.AppleAppId)throw new StoreVerificationException();
    }
}
public sealed class AppleStoreClient(StoreOptions options,StoreHttp http,AppleSignedDataVerifier signatures)
{
    public async Task<VerifiedPurchase> VerifyAsync(Guid user,string evidence,string? expectedProduct,CancellationToken ct)
    {
        if(!options.AppleEnabled)throw new StoreUnavailableException();
        using var input=signatures.Verify(evidence);signatures.App(input.RootElement);
        if(input.RootElement.GetProperty("productId").GetString()!=expectedProduct)throw new StoreVerificationException();
        return await FetchAsync(user,input.RootElement.GetProperty("originalTransactionId").GetString()!,ct);
    }
    public async Task<VerifiedPurchase> FetchAsync(Guid user,string original,CancellationToken ct)
    {
        if(!options.AppleEnabled)throw new StoreUnavailableException();
        using var key=ECDsa.Create();key.ImportFromPem(options.ApplePrivateKeyPem);
        var signing=new ECDsaSecurityKey(key){KeyId=options.AppleKeyId};var now=DateTime.UtcNow;
        var jwt=new JwtSecurityToken(options.AppleIssuerId,"appstoreconnect-v1",[new Claim("bid",options.AppleBundleId)],now,now.AddMinutes(5),new SigningCredentials(signing,SecurityAlgorithms.EcdsaSha256));
        var host=options.AppleEnvironment=="Production"?"https://api.storekit.apple.com":"https://api.storekit-sandbox.apple.com";
        using var request=StoreHttp.Get(host+"/inApps/v1/subscriptions/"+Uri.EscapeDataString(original),new JwtSecurityTokenHandler().WriteToken(jwt));
        using var response=await http.JsonAsync(request,ct);var root=response.RootElement;
        if(root.GetProperty("bundleId").GetString()!=options.AppleBundleId||root.GetProperty("environment").GetString()!=options.AppleEnvironment
            ||options.AppleEnvironment=="Production"&&root.GetProperty("appAppleId").GetInt64()!=options.AppleAppId)throw new StoreVerificationException();
        VerifiedPurchase? latest=null;long latestSigned=long.MinValue;
        foreach(var group in root.GetProperty("data").EnumerateArray())foreach(var item in group.GetProperty("lastTransactions").EnumerateArray())
        {
            using var transaction=signatures.Verify(item.GetProperty("signedTransactionInfo").GetString()!);
            using var renewal=signatures.Verify(item.GetProperty("signedRenewalInfo").GetString()!);
            var t=transaction.RootElement;var r=renewal.RootElement;signatures.App(t);
            if(t.GetProperty("originalTransactionId").GetString()!=original||r.GetProperty("originalTransactionId").GetString()!=original
                ||r.GetProperty("environment").GetString()!=options.AppleEnvironment)continue;
            var product=t.GetProperty("productId").GetString()!;
            if(!options.AppleProducts.Contains(product,StringComparer.Ordinal)||!Guid.TryParse(t.GetProperty("appAccountToken").GetString(),out var account)||account!=user)throw new StoreVerificationException();
            var status=item.GetProperty("status").GetInt32();var state=status switch {1=>MembershipState.Active,2=>MembershipState.Expired,3=>MembershipState.BillingRetry,4=>MembershipState.GracePeriod,5=>MembershipState.Revoked,_=>MembershipState.Expired};
            if(t.TryGetProperty("revocationDate",out _))state=MembershipState.Revoked;
            var expiry=t.GetProperty("expiresDate").GetInt64();if(state==MembershipState.GracePeriod)expiry=r.GetProperty("gracePeriodExpiresDate").GetInt64();
            var signed=t.GetProperty("signedDate").GetInt64();if(signed<latestSigned)continue;latestSigned=signed;
            latest=new("Apple",product,original,t.GetProperty("transactionId").GetString()!,null,null,account,state,DateTimeOffset.FromUnixTimeMilliseconds(expiry).UtcDateTime,r.GetProperty("autoRenewStatus").GetInt32()==1,options.AppleEnvironment,original);
        }
        return latest??throw new StoreVerificationException();
    }
}
