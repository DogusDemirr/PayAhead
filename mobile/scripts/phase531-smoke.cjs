// Yalnız loopback Development; sadece kendi geçici hesabını ve kayıtlarını temizler.
const { randomUUID, randomBytes } = require('node:crypto');
const sharp = require('sharp');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5094');
  if (!['localhost', '127.0.0.1'].includes(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw Error('Yalnız loopback API kökü kabul edilir.');
  const body = { email: `phase531-${randomUUID()}@example.invalid`, password: randomBytes(24).toString('base64url'), displayName: 'Disposable Phase531' };
  let id, token, checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); };
  async function call(path, method = 'GET', data, expected = 200, version) {
    const multipart = data instanceof FormData;
    const response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(!multipart && data ? { 'Content-Type': 'application/json' } : {}), ...(version ? { 'If-Match': `"${version}"` } : {}) }, body: multipart ? data : data ? JSON.stringify(data) : undefined });
    assert(response.status === expected, `${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}: expected ${expected}, got ${response.status}`); checks++;
    if (response.headers.get('Content-Type')?.startsWith('image/')) return response;
    const raw = await response.text(); return raw ? JSON.parse(raw) : null;
  }
  function upload(bytes) { const form = new FormData(); form.append('image', new Blob([bytes], { type: 'application/octet-stream' }), '../../untrusted.svg'); return form; }
  try {
    await call('/health/ready'); id = (await call('/api/auth/register', 'POST', body, 201)).id;
    token = (await call('/api/auth/login', 'POST', { email: body.email, password: body.password })).accessToken;
    const write = { name: 'Disposable plan', amount: 12.3456, currencyCode: 'USD', category: 'Other', frequency: 'Monthly', interval: 1, startDate: '2026-10-01', anchorDay: 1, isActive: true };
    for (const active of [false, true]) {
      const plan = await call('/api/payments', 'POST', { ...write, isActive: active }, 201);
      await call(`/api/payments/${plan.id}`, 'DELETE', null, 204, plan.rowVersion);
      assert((await call('/api/payments')).totalCount === 0, 'Deleted plan remained in list');
    }
    const plan = await call('/api/payments', 'POST', write, 201);
    const changed = await call(`/api/payments/${plan.id}`, 'PUT', { ...write, isActive: false, rowVersion: plan.rowVersion });
    const stale = await call(`/api/payments/${plan.id}`, 'DELETE', null, 409, plan.rowVersion); assert(stale.code === 'payment_changed', 'Conflict code mismatch');
    assert((await call('/api/payments')).totalCount === 1, 'Conflict removed plan');
    await call(`/api/payments/${plan.id}`, 'DELETE', null, 204, changed.rowVersion);
    const sub = await call('/api/subscriptions', 'POST', { name: 'Disposable linked', providerName: 'Disposable', amount: 1, currencyCode: 'USD', billingFrequency: 'Monthly', billingInterval: 1, anchorDay: 1, startDate: '2026-10-01', firstBillingDate: '2026-11-01', status: 'Active', autoRenew: true, enableTrialReminders: false }, 201);
    const linked = await call(`/api/payments/${sub.paymentPlanId}`);
    const blocked = await call(`/api/payments/${linked.id}`, 'DELETE', null, 409, linked.rowVersion); assert(blocked.code === 'payment_linked_subscription', 'Linked restriction code mismatch');
    const profile = await call('/api/users/me/profile'); const png = await sharp({ create: { width: 40, height: 30, channels: 3, background: '#0f766e' } }).png().toBuffer();
    const saved = await call('/api/users/me/avatar', 'PUT', upload(png), 200, profile.rowVersion); assert(saved.hasAvatar, 'Avatar not saved');
    const image = await call('/api/users/me/avatar'); assert(image.headers.get('Cache-Control').includes('no-store'), 'Private cache policy missing');
    const metadata = await sharp(Buffer.from(await image.arrayBuffer())).metadata(); assert(metadata.width === 512 && metadata.height === 512 && metadata.format === 'jpeg' && !metadata.exif, 'Avatar optimization mismatch');
    token = (await call('/api/auth/login', 'POST', { email: body.email, password: body.password })).accessToken;
    assert((await call('/api/users/me')).hasAvatar, 'Avatar not persistent after login');
    await call('/api/users/me/avatar', 'PUT', upload(Buffer.from('<svg/>')), 400, saved.rowVersion);
    const restored = await call('/api/users/me/profile'); assert(restored.hasAvatar, 'Invalid upload removed photo');
    const removed = await call('/api/users/me/avatar', 'DELETE', null, 200, restored.rowVersion); assert(!removed.hasAvatar, 'Avatar not removed'); await call('/api/users/me/avatar', 'GET', null, 404);
    console.log(`Phase 5.3.1 gerçek HTTP: ${checks} kontrol başarılı; silme/concurrency/bağlı kayıt ve avatar akışı doğrulandı.`);
  } finally {
    if (id) { if (!token) token = (await call('/api/auth/login', 'POST', { email: body.email, password: body.password })).accessToken; assert((await call('/api/users/me')).id === id, 'Cleanup ownership mismatch'); await call('/api/users/me', 'DELETE', { currentPassword: body.password }, 204); console.log('Yalnız smoke hesabı ve kendi kayıtları temizlendi.'); }
  }
}
main().catch(error => { if (/^(GET|POST|PUT|DELETE) \/api\//.test(error.message)) console.error(error.message); console.error('Phase 5.3.1 smoke başarısız. Yanıt gövdeleri ve kimlik bilgileri yazdırılmadı.'); process.exitCode = 1; });
