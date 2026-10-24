// Controlled local API test. Synthetic tokens are never sent to Expo; worker must be disabled.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  if (process.env.PAYAHEAD_NOTIFICATION_SMOKE_WORKER_DISABLED !== 'true') throw new Error('Önce yerel API worker kapalı olarak başlatılmalı; PAYAHEAD_NOTIFICATION_SMOKE_WORKER_DISABLED=true açık onayı gerekli.');
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  if (!/^(localhost|127\.0\.0\.1|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[01])\.\d+\.\d+)$/.test(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw new Error('Yalnızca yerel/LAN API kabul edilir.');
  const accounts = []; let count = 0;
  const assert = (ok, message) => { if (!ok) throw new Error(message); };
  async function call(path, method, body, token, expected) {
    let response; try { response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body ? JSON.stringify(body) : undefined }); } catch { throw new Error('Yerel API bağlantısı kurulamadı.'); }
    const label = `${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}`;
    assert(response.status === expected, `${label}: beklenen ${expected}, alınan ${response.status}`); count++; console.log(`${label}: ${response.status}`); const raw = await response.text(); return { raw, data: raw ? JSON.parse(raw) : null };
  }
  async function account() { const body = { email: `notification-smoke-${randomUUID()}@example.invalid`, displayName: 'Disposable notification smoke', password: randomBytes(24).toString('base64url') }; const entry = { body, id: null, auth: null }; accounts.push(entry); entry.id = (await call('/api/auth/register', 'POST', body, null, 201)).data.id; entry.auth = (await call('/api/auth/login', 'POST', { email: body.email, password: body.password }, null, 200)).data; return entry; }
  try {
    for (const path of ['/api/devices', '/api/users/me/settings']) await call(path, 'GET', null, null, 401);
    const owner = await account(); const token = owner.auth.accessToken;
    const settings = (await call('/api/users/me/settings', 'GET', null, token, 200)).data;
    const entitlement = (await call('/api/premium/entitlements', 'GET', null, token, 200)).data; assert(entitlement.limits.activeRemindersPerPlan === 1 && !entitlement.isPremium, 'Free reminder1 doğrulanamadı.');
    const { usedTimeZoneFallback, ...settingsWrite } = settings;
    await call('/api/users/me/settings', 'PUT', { ...settingsWrite, timeZoneId: 'America/New_York', notificationsEnabled: false }, token, 200);
    await call('/api/users/me/settings', 'PUT', { ...settingsWrite, timeZoneId: 'invalid/zone' }, token, 400);
    await call('/api/users/me/settings', 'PUT', { ...settingsWrite, timeZoneId: 'Europe/Istanbul', notificationsEnabled: true }, token, 200);
    const today = (await call('/api/dashboard/summary', 'GET', null, token, 200)).data.context.today;
    const payment = (await call('/api/payments', 'POST', { name: 'Disposable reminder plan', description: null, amount: 1, currencyCode: 'TRY', category: 'Smoke', frequency: 'Monthly', interval: 1, startDate: today, endDate: null, anchorDay: null, isActive: true }, token, 201)).data;
    const path = `/api/payments/${payment.id}/reminders`;
    await call(path, 'GET', null, token, 200);
    const rule = (await call(path, 'POST', { daysBefore: 3, timeOfDay: '09:00', isEnabled: true }, token, 201)).data;
    await call(path, 'POST', { daysBefore: 3, timeOfDay: '09:00', isEnabled: true }, token, 409);
    assert((await call(path, 'POST', { daysBefore: 7, timeOfDay: '09:00', isEnabled: true }, token, 403)).data.code === 'entitlement_limit', 'Limit kodu eşleşmedi.');
    await call(path, 'POST', { daysBefore: 366, timeOfDay: '09:00' }, token, 400); await call(path, 'POST', { daysBefore: 0, timeOfDay: '24:00' }, token, 400);
    await call(path + '/' + rule.id, 'PUT', { daysBefore: 0, timeOfDay: '02:30', isEnabled: false }, token, 200);
    const syntheticToken = `ExpoPushToken[${randomBytes(24).toString('base64url')}]`;
    const registered = await call('/api/devices', 'POST', { platform: 'android', pushToken: syntheticToken }, token, 200); const device = registered.data; assert(!registered.raw.includes(syntheticToken), 'Device response token sızdırıyor.');
    await call('/api/devices', 'POST', { platform: 'android', pushToken: `ExpoPushToken[${randomBytes(24).toString('base64url')}]`, id: device.id }, token, 200);
    await call('/api/devices', 'POST', { platform: 'android', pushToken: syntheticToken, id: device.id }, token, 200);
    const other = await account(); const otherToken = other.auth.accessToken;
    await call(path, 'GET', null, otherToken, 404); await call(path + '/' + rule.id, 'PUT', { daysBefore: 0, timeOfDay: '09:00' }, otherToken, 404); await call(path + '/' + rule.id, 'DELETE', null, otherToken, 404);
    await call('/api/devices/' + device.id, 'DELETE', null, otherToken, 404); await call('/api/devices', 'POST', { platform: 'android', pushToken: syntheticToken }, otherToken, 409);
    assert((await call('/api/devices', 'GET', null, otherToken, 200)).data.length === 0, 'Cihaz kullanıcı izolasyonu bozuldu.');
    await call(path + '/' + rule.id, 'DELETE', null, token, 204); await call('/api/devices/' + device.id, 'DELETE', null, token, 204);
    assert((await call('/api/devices', 'GET', null, token, 200)).data.length === 0, 'Cihaz kaldırılmadı.');
    console.log(`Gerçek bildirim yönetimi API smoke: ${count} HTTP kontrolü başarılı. Worker kapalı, sentetik token yalnız API testi; gerçek Expo gönderimi/cihaz teslimi test edilmedi.`);
  } finally {
    let failed = false;
    for (const entry of accounts.reverse()) { if (!entry.id) continue; try { if (!entry.auth) entry.auth = (await call('/api/auth/login', 'POST', { email: entry.body.email, password: entry.body.password }, null, 200)).data; assert((await call('/api/users/me', 'GET', null, entry.auth.accessToken, 200)).data.id === entry.id, 'Cleanup sahipliği'); await call('/api/users/me', 'DELETE', { currentPassword: entry.body.password }, entry.auth.accessToken, 204); } catch { failed = true; console.error('Testin oluşturduğu notification-smoke-* hesabının temizliği doğrulanamadı.'); } }
    if (failed) throw new Error('Smoke cleanup tamamlanamadı.'); console.log('Yalnızca testin kendi hesapları temizlendi.');
  }
}
main().catch(e => { console.error(e.message); process.exitCode = 1; });
