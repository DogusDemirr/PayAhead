# API sözleşmesi — Faz 1

| Uç | Başarı | Hata | Kimlik |
| --- | --- | --- | --- |
| GET /health/live | 200, `{ "status": "Healthy" }` | — | Anonim |
| GET /health/ready | 200, `{ "status": "Healthy" }` | 503 ProblemDetails | Anonim |
| GET /swagger/v1/swagger.json | 200 OpenAPI | Development dışında yok | Yalnızca Development |

Readiness SQL bağlantısı ve uygulanmamış migration kontrolü yapar. Genel hata gövdesi application/problem+json kullanır; traceId tanılama içindir. 500 yanıtı iç hata mesajı veya stack trace içermez. Varsayılan politika kimlik doğrulama ister; Faz 1'de iş kaynağı endpoint'i yoktur.

Ödeme CRUD Faz 2; takvim/özet Faz 5; register/login/refresh/logout/me Faz 6 kapsamındadır. Bu uçlar henüz mevcut değildir. Koleksiyonlar ileride sayfalı DTO döndürmeli, sahip olunmayan kaynak 404 olmalı, concurrency çatışmaları 409 dönmelidir.
