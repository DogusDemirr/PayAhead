# Üretime hazırlık kontrolü

21 Ekim2026. Completed yalnızca yazılı yerel/kod kapsamını anlatır. Production üzerinde denenmemiş madde Completed değildir.

| Madde | Durum | Kanıt / kalan iş |
| --- | --- | --- |
| Backend kodu / yerel regresyonlar | Completed | Güncel gerçek sonuçlar IMPLEMENTATION_PROGRESS |
| Fail-fast / fake sınırları | Completed | Yerel unit/SQL testleri |
| Docker non-root/read-only/shutdown | Completed | Yerel Testing container; production TLS değil |
| JWT production secret | Pending | Dış CSPRNG key ve rotasyon tatbikatı |
| SQL production connection | Pending | En az yetkili hesap, güvenilir TLS, pooling |
| Apple credentials / ürünler / gerçek satın alma | Blocked | Credential ve mağaza kurulumu yok |
| Google credentials / IAM / satın alma / acknowledgement | Blocked | Credential ve Play kurulumu yok |
| Expo credentials / fiziksel teslimat | Blocked | Gerçek cihaz doğrulaması yok |
| HTTPS/domain/trusted proxy | Pending | Gerçek TLS/firewall/proxy smoke |
| Production database backup | Pending | Şifreleme, RPO/RTO, zamanlama, ayrı erişim alanı |
| Production restore testi | Pending | Yerel izole backup/restore/CHECKDB geçti; production değil |
| GitHub CI/CD | Pending | Workflow/actionlint hazır; GitHub run/required checks/approval ayarı yok |
| Health checks | Pending | Yerel live/ready/worker; gerçek probe/alert kurulumu |
| Distributed rate limiting | Pending | SQL çoklu connection/iki API testi; production sizing/WAF/key birliği kurulmalı |
| Monitoring | Pending | Meter/Activity/log var; collector/exporter/dashboard/alerts kurulmalı |
| Store webhook setup | Blocked | Gerçek RTDN/V2 routing ve retry denemesi yok |
| Mobile app integration | Pending | API sözleşmesi hazır; frontend/SDK satın alma testi yok |
| Account deletion | Pending | Yerel reauth/transaction/isolation/rollback; production retention/support onayı |
| Privacy policy / KVKK ve uygulanabilir hukuk | Pending | Yasal dayanak, metin, retention süreleri uzman onayı bekliyor |
| OS/container vulnerability scan | Pending | NuGet audit temiz; base-image/OS ayrıca taranmalı |
| Production kapasite/SLO | Pending | Yerel100/500 burst; soak/sustained/gerçek donanım yok |
| .NET8 destek süresi sonrası işletim | Pending |10 Kasım2026 sonrası onaylı LTS planı |

Frontend test API sözleşmesiyle başlayabilir. Liste production release onayı veya hukuki uyumluluk garantisi değildir. Production'a deploy edilmedi.
