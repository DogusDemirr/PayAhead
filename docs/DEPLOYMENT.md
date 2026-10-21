# Dağıtım hazırlığı — Faz 10

Production deployment yapılmadı. Yerel doğrulamalar gerçek domain, credential ve CI çalışması yerine geçmez; [checklist](PRODUCTION_CHECKLIST.md) dış ortam kapıları ayrıca kapatılmalıdır.

## Ortam / secret / ağ

Development yereldir; eksik JWT key süreç ömürlü üretilir, Swagger açıktır. Testing açık JWT key ister, Swagger kapalıdır ve izole DB kullanmalıdır. Staging/Production güçlü dış JWT key, açık AllowedHosts, Encrypt=True/Strict + TrustServerCertificate=False ve SA olmayan SQL kimliği gerektirir. Bilinmeyen environment reddedilir. Shipping API fake store sağlayamaz; production fake push bayrağı ve etkin resmi Expo dışı endpoint reddedilir. DevelopmentIdentity Development dışında açılamaz; Development'ta da API kimlik sağlamaz.

`docker/production.env.example` bilerek eksiktir, gerçek secret içermez. Secret manager veya servis hesabına özel dış env file kullanın. JWT en az32 CSPRNG byte Base64; SQL password, Apple/Google PEM, Expo token ve store AES keyring Git/image dışında tutulur. Optional store/push kapalı başlayabilir; etkin entegrasyon temel config/key olmadan başlamaz. Gerçek mağaza/IAM kontrolü [STORE_INTEGRATION.md](STORE_INTEGRATION.md).

İş API'si Staging/Production/Testing'de HTTPS'e yönlenir, HTTPS'te HSTS vardır. TLS reverse proxy'de sonlandırılır; backend internete açılmaz. TrustedProxies yalnızca gerçek son proxy IP'si, ForwardLimit=1; allowlist yoksa forwarded headers kullanılmaz. Proxy gelen X-Forwarded-* değerlerini silip yeniden üretmelidir; doğrudan backend erişimi ağda kapanmalıdır. [Microsoft proxy rehberi](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer).

`/health/live`, `/health/ready`, `/health/workers` iç HTTP probe için redirect dışında, minimal anonim durumdur; proxy bunları halka kapatabilir. Live süreç, ready SQL/pending migration, workers ayrı heartbeat kontrolüdür. Dış Apple/Google/Expo kesintisi bütün API'yi readiness'ten düşürmez. CORS exact HTTPS origin allowlist; varsayılan boş, credentials yok, wildcard yok. Native mobil CORS güvenlik sınırı değildir. Swagger yalnızca Development; diğer ortamda anonim fallback401, JWT ile404.

## Docker / migration

```powershell
docker build -f docker/Dockerfile -t payahead:reviewed .
# Bu makinedeki .tools SDK ve yerel .env SQL ile izole doğrulama:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/local-container-smoke.ps1
```

Multi-stage SDK8/runtime8, yalnızca src publish çıktısı; non-root app/UID1654, dahili .NET health probe, SIGTERM ve30s host shutdown. Yerel read-only root +64MiB tmpfs,768MiB RAM,2CPU,128PID,cap_drop ALL,no-new-privileges,init geçti. DOTNET_EnableDiagnostics=0 read-only işletim için kullanılır; collector/socket gereksinimi staging'de ayrıca değerlendirilmelidir. Runtime'a SDK/test/secret girmez. [Microsoft container rehberi](https://learn.microsoft.com/en-us/dotnet/core/docker/build-container).

`docker/compose.production.example.yml` geliştirme Compose'dan ayrıdır; dış immutable image digest ve secret env file ister. Production SQL Developer Compose değildir. Runtime en az data CRUD yetkili, migration ayrı DDL hesabıyla. Startup migration çalıştırmaz. Build8.0 kanalından patch alır; yayınlanan image digest kaydedilip deployment'a sabitlenmelidir. OS/base-image CVE taraması ayrıca gerekir; temiz NuGet audit bunun yerine geçmez.

## CI / manuel release

CI PR/main/master/manual: restore, whitespace check, Release build, unit, gerçek SQL Testcontainers, fail-closed NuGet advisory kontrolü, Docker build. Başarısız test sonraki release adımını engeller. Test artifact3gün; manuel CI'da ayrıca image tar.gz, registry push/deploy yok. Permissions contents:read, checkout credentials kalıcı değil, actions resmi repo tag'lerinden doğrulanmış full SHA'ya pin'li. [GitHub güvenli kullanım](https://docs.github.com/en/actions/reference/security/secure-use).

GitHub workflow çalıştırılmadı; yerel actionlint geçti. Branch protection/required CI checks ve production environment required reviewers repository sahibince kurulmalıdır. `release-review.yml` sadece workflow_dispatch + production environment approval kaydı örneğidir, deploy komutu yoktur. Repo environment koruması kurulmadan approval garantisi vermez. Production secret'ları protected environment'ta tutun, self-review kapatın.

Yayın sırası: test/scan → immutable image → staging backup/restore → idempotent migration script review → ayrı migration hesabı → staging smoke/load → checklist/manual approval → ayrıca yetkilendirilmiş deployment. Hata halinde uyumlu önceki image; otomatik Down yok, ileri düzeltme veya doğrulanmış restore. Production veri silinmedi.

.NET8/C#12/EF8 korundu. .NET8 desteği10 Kasım2026'da biter; 21 Ekim2026 itibarıyla güncel runtime8.0.32. [Microsoft destek politikası](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Destek süresi sonrası işletim için ayrı onaylı LTS geçişi planlanmalı; .NET9/10'a geçilmedi.
