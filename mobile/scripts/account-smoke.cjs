const { registrationLegal } = require('./registration-fixture.cjs');
// Development localhost only. Disposable accounts; no credentials or response bodies are printed.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5093');
  if (!['localhost', '127.0.0.1'].includes(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw Error('Yalnız localhost API kökü kabul edilir.');
  const assert = (ok, message) => { if (!ok) throw Error(message); }; const accounts = []; let checks = 0;
  async function call(path, method = 'GET', body, token, expected = 200) {
    let response; try { response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body ? JSON.stringify(body) : undefined }); } catch { throw Error('Yerel API bağlantısı kurulamadı.'); }
    assert(response.status === expected, `${method} ${path}: beklenen ${expected}, alınan ${response.status}`); checks++;
    const raw = await response.text(); return raw ? JSON.parse(raw) : null;
  }
  async function login(entry, password = entry.password, expected = 200) { return call('/api/auth/login', 'POST', { email: entry.email, password }, null, expected); }
  async function register() {
    const entry = { email: `account-smoke-${randomUUID()}@example.invalid`, password: randomBytes(24).toString('base64url'), displayName: 'Disposable profile account', id: null, token: null, deleted: false }; accounts.push(entry);
    entry.id = (await call('/api/auth/register', 'POST', { email: entry.email, password: entry.password, displayName: entry.displayName, ...registrationLegal() }, null, 201)).id;
    const auth = await login(entry); entry.token = auth.accessToken; entry.refresh = auth.refreshToken; return entry;
  }
  try {
    await call('/health/ready'); await call('/api/users/me/profile', 'GET', null, null, 401);
    const owner = await register(); const other = await register(); const original = await call('/api/users/me/profile', 'GET', null, owner.token);
    const body = { displayName: 'Changed disposable name', rowVersion: original.rowVersion };
    const changed = await call('/api/users/me/profile', 'PUT', body, owner.token); assert(changed.id === owner.id && changed.email === owner.email && changed.rowVersion !== original.rowVersion, 'Profil sözleşmesi/concurrency eşleşmedi.');
    await call('/api/users/me/profile', 'PUT', body, owner.token, 409); await call('/api/users/me/profile', 'PUT', { ...body, rowVersion: changed.rowVersion, displayName: ' ' }, owner.token, 400);
    await call('/api/users/me/profile', 'PUT', { ...body, userId: other.id }, owner.token, 400); await call('/api/users/me/profile', 'PUT', { ...body, email: other.email }, owner.token, 400);
    assert((await call('/api/users/me', 'GET', null, owner.token)).displayName === body.displayName, 'Profil kalıcılığı doğrulanamadı.');
    assert((await call('/api/users/me/profile', 'GET', null, other.token)).displayName === other.displayName, 'Diğer hesap değişti.');
    const extraSession = await login(owner); const newPassword = randomBytes(24).toString('base64url');
    const wrong = await call('/api/users/me/change-password', 'POST', { currentPassword: 'incorrect-password', newPassword, confirmation: newPassword }, owner.token, 400); assert(wrong.code === 'current_password_invalid', 'Yanlış parola hata kodu eşleşmedi.');
    await call('/api/users/me', 'GET', null, owner.token);
    await call('/api/users/me/change-password', 'POST', { currentPassword: owner.password, newPassword, confirmation: newPassword }, owner.token, 204);
    const oldPassword = owner.password; owner.password = newPassword;
    await call('/api/users/me', 'GET', null, owner.token, 401); await call('/api/users/me', 'GET', null, extraSession.accessToken, 401);
    await call('/api/auth/refresh', 'POST', { refreshToken: owner.refresh }, null, 401); await login(owner, oldPassword, 401);
    const newAuth = await login(owner); owner.token = newAuth.accessToken; owner.refresh = newAuth.refreshToken;
    assert((await call('/api/users/me/profile', 'GET', null, owner.token)).displayName === body.displayName, 'Yeni girişte profil korunmadı.');
    const entitlement = await call('/api/premium/entitlements', 'GET', null, owner.token); assert(entitlement.isPremium === false && entitlement.plan === 'Free', 'Gerçek Free entitlement bekleniyordu.');
    const settings = await call('/api/users/me/settings', 'GET', null, owner.token);
    await call('/api/users/me/language', 'PUT', { languageCode: 'de' }, owner.token);
    await call('/api/users/me/settings', 'PUT', { languageCode: 'de', defaultCurrencyCode: settings.defaultCurrencyCode, timeZoneId: 'UTC', notificationsEnabled: settings.notificationsEnabled }, owner.token);
    await call('/api/users/me', 'DELETE', { currentPassword: owner.password }, owner.token, 204); owner.deleted = true;
    await call('/api/users/me', 'GET', null, owner.token, 401); await call('/api/auth/refresh', 'POST', { refreshToken: owner.refresh }, null, 401);
    console.log(`Phase 5.3 gerçek HTTP smoke: ${checks} kontrol başarılı.`);
  } finally {
    let failed = false;
    for (const entry of accounts) if (entry.id && !entry.deleted) {
      try { const auth = await login(entry); assert((await call('/api/users/me', 'GET', null, auth.accessToken)).id === entry.id, 'Cleanup sahipliği'); await call('/api/users/me', 'DELETE', { currentPassword: entry.password }, auth.accessToken, 204); entry.deleted = true; }
      catch { failed = true; console.error('Yalnız testin oluşturduğu account-smoke hesabının temizliği doğrulanamadı.'); }
    }
    if (failed) throw Error('Smoke cleanup eksik.'); console.log('Yalnız testin kendi geçici hesapları temizlendi.');
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
