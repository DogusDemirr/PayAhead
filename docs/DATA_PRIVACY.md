# Veri export ve hesap silme

Phase 5.4.1: Kendi export'una `registrationLegal` belge ID/tür/sürüm/server UTC kayıtları eklenir. İlk iki tür bilgilendirme/okundu teyidi, üçüncüsü terms kabulüdür; genel veri işleme rızası değildir. Hesap silme yalnız bu kullanıcıya ait yeni satırları da temizler. Mevcut kullanıcı ve eski receipt'ler backfill edilmez. [Güncel saklama ve hukuki sınırlar](PHASE5_4_1_REPORT.md).

Phase 5.4: Export'a yalnız `legalNotice` sürüm/sunum/kayıt metadata'sı eklenir; parola geçmişi/özetleri export edilmez. Doğrulamalı silme bu kullanıcıya ait `PasswordHistory` ve `LegalNoticeReceipts` satırlarını da temizler. Kayıt bilgilendirmesi açık rıza değildir. [Saklama ve bilinmeyen yayın bilgileri](PRIVACY_SECURITY.md), [hukuki taslaklar](legal/KVKK_AYDINLATMA_TASLAK.md).

Teknik davranış açıklanır; KVKK veya başka uygulanabilir mevzuata uygunluk garanti edilmez. Veri sorumlusu, aydınlatma, işleme/aktarım dayanağı, saklama ve silme yükümlülükleri hukuk/gizlilik sahibi tarafından incelenmelidir. [KVKK resmi kaynakları](https://www.kvkk.gov.tr/).

## Export

GET `/api/users/me/export`: JWT + aktif kullanıcı, ortak privacy5/dakika. JSON attachment `payahead-export.json`, no-store, schemaVersion1 ve exportedAtUtc. Kendi profile/settings/plans/occurrences/reminders/trackedSubscriptions/membership/device metadata. DTO allowlist: password hash, access/refresh/push/purchase token, token hash, protected evidence, credential ve başka kullanıcı verisi yoktur.

Bounded SERIALIZABLE transaction. En fazla1000plan/20000occurrence/10000reminder/1000tracked/100membership/1000device metadata ve8MiB çıktı. Aşım413, sessiz truncation yok. Büyük hesap için yetkili destek üzerinden parçalı aktarım gerekir; async export servisi yoktur. Bounded materyalizasyon yine serializer boyut kontrolünden önce geçici bellek kullanır. Query UserId sahipliği değiştirmez.

## Silme

DELETE `/api/users/me`, JWT ve `{"currentPassword":"..."}` zorunlu. Güncel parola kilit altında Identity PasswordHasher ile tekrar doğrulanır; en fazla4096byte body. Doğru204, yanlış/eksik401. Privacy5/dakika ortak bütçe. Tek transaction: user inactive + email/name/password scrub; refresh revoke/delete; device/delivery/pending işler/rules/tracked/occurrence/payment history/settings/user audit silme. User ve plan kilitleri, FK sırası korunur. Store encrypted evidence silinir, verification/periodic sorgu kapanır; sahipliği bilinen inbox referansları kaldırılır. Hata rollback, başka kullanıcı etkilenmez.

Pasif pseudonymous UUID ve mağaza üyelik/transaction/token-hash anti-replay bağları kalır. Bunlar anonim sayılmamalı, ilişkilendirilebilir verilerdir. Hesaplar arası satın alma reuse önleme için tutulur; finansal/yasal saklama zorunluluğu varsayılmaz. Production öncesi dayanak/süre/erişim/purge kararını hukuk/gizlilik sahibi onaylamalıdır; otomatik süresiz saklama hukuki onay değildir (checklist Pending).

Servis tekrar silme idempotent; HTTP sonrası eski JWT aktif kullanıcı kontrolünde401, yeni login/refresh reddedilir. Aynı email yeni UUID olur; eski store satın alma aktarılmaz. Silme geri alınamaz, export önceden alınmalıdır. Mevcut veriye kendiliğinden silme uygulanmaz; yalnızca açık, doğrulanmış DELETE.

Hesap silme App Store/Play aboneliğini iptal etmez; kullanıcı auto-renew'i mağazada ayrıca kapatmalıdır. Frontend onay ekranı bu durumu ve geçmiş kaybını açıklamalıdır. In-flight kabul edilmiş push geri alınamaz; exactly-once/geri çekme garantisi yoktur. Silme sonrası yeni store bildirimi inbox'a tekrar yazılabilir; inactive üyelik hak veya yeniden provider sorgusu üretmez, retention ayrıca incelenmelidir.

Eski backup silinen veriyi taşıyabilir. Backup retention/erişim ayrı yönetilir; restore sonrası canlıya açmadan önce silme talepleri tekrar uygulanmalıdır. Harici deletion-ledger servisi bu fazda yoktur. Şifreli backup, ayrı keyring, log redaction ve operatör erişim kaydı gerekir. Uygulama metric label'ları kişisel veri içermez.
