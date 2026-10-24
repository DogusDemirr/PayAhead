// Yalnızca yerel Development API; sadece kendi oluşturduğu hesapları temizler. Token/veri gövdeleri yazdırılmaz.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  if (!/^(localhost|127\.0\.0\.1|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[01])\.\d+\.\d+)$/.test(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw new Error('Yalnızca yerel/LAN API kökü kabul edilir.');
  const accounts = []; let checks = 0;
  function assert(condition, message) { if (!condition) throw new Error(message); }
  async function call(path, method, body, token, status, version) {
    const json = body && 'amount' in body ? (() => { const { amount, ...rest } = body; return JSON.stringify(rest).slice(0, -1) + `,"amount":${amount}}`; })() : body ? JSON.stringify(body) : undefined;
    let response;
    try { response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { Accept: 'application/json', ...(json ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(version ? { 'If-Match': `"${version}"` } : {}) }, body: json }); }
    catch { throw new Error('Yerel API bağlantısı kurulamadı.'); }
    const label = `${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}`;
    assert(response.status === status, `${label}: beklenen ${status}, alınan ${response.status}.`); checks++; console.log(`${label}: ${response.status}`);
    const raw = await response.text(); return { raw, data: raw ? JSON.parse(raw) : null };
  }
  async function account() {
    const body = { email: `subscription-smoke-${randomUUID()}@example.invalid`, displayName: 'Disposable subscription smoke', password: randomBytes(24).toString('base64url') };
    const entry = { body, id: null, auth: null }; accounts.push(entry);
    entry.id = (await call('/api/auth/register', 'POST', body, null, 201)).data.id;
    entry.auth = (await call('/api/auth/login', 'POST', { email: body.email, password: body.password }, null, 200)).data; return entry;
  }
  const day = (date, delta) => { const d = new Date(date + 'T12:00:00Z'); d.setUTCDate(d.getUTCDate() + delta); return d.toISOString().slice(0, 10); };
  try {
    for (const path of ['/api/subscriptions', '/api/subscriptions/summary', '/api/subscriptions/trials/expiring']) await call(path, 'GET', null, null, 401);
    const owner = await account(); const token = owner.auth.accessToken;
    const initial = (await call('/api/subscriptions/summary', 'GET', null, token, 200)).data; const today = initial.context.today;
    const ent = (await call('/api/premium/entitlements', 'GET', null, token, 200)).data;
    assert(!ent.isPremium && ent.limits.activeTrackedSubscriptions === 3, 'Free test limitinin 3 olduğu doğrulanamadı.');
    const body = { name: 'Phase5 trial', providerName: 'Disposable provider', description: 'Disposable', amount: '199.9001', currencyCode: 'TRY', billingFrequency: 'Monthly', billingInterval: 1, anchorDay: null, startDate: today, firstBillingDate: day(today, 8), trialStartDate: today, trialEndDate: day(today, 7), status: 'Trial', autoRenew: true, accessEndsOn: null, enableTrialReminders: true };
    const created = await call('/api/subscriptions', 'POST', body, token, 201); let record = created.data;
    assert(created.raw.includes('199.9001') && record.trialDaysRemaining === 7 && record.nextBillingDate === body.firstBillingDate, 'Trial/decimal sözleşmesi eşleşmedi.');
    const plans = (await call('/api/payments?pageSize=100', 'GET', null, token, 200)).data; assert(plans.totalCount === 1 && plans.items[0].id === record.paymentPlanId, 'Tek bağlı ödeme planı kuralı bozuldu.');
    await call(`/api/subscriptions/${record.id}`, 'GET', null, token, 200);
    const trials = (await call('/api/subscriptions/trials/expiring?withinDays=7', 'GET', null, token, 200)).data; assert(trials.length === 1 && trials[0].firstBillingDate !== trials[0].trialEndDate, 'Deneme ve ücretli tarih ayrımı eşleşmedi.');
    assert((await call('/api/subscriptions/trials/expiring?withinDays=3', 'GET', null, token, 200)).data.length === 0, '3 gün filtresi eşleşmedi.');
    const [year, month] = body.firstBillingDate.split('-').map(Number);
    const calendar = (await call(`/api/calendar?year=${year}&month=${month}`, 'GET', null, token, 200)).data;
    const linked = calendar.days.flatMap(x => x.payments).filter(x => x.paymentPlanId === record.paymentPlanId); assert(linked.length === 1 && linked[0].dueDate === body.firstBillingDate, 'Takvim tek kaynak/ilk ücretli tarih kuralı bozuldu.');
    await call('/api/subscriptions', 'POST', { ...body, name: 'Phase5 USD', amount: '120', currencyCode: 'USD', billingFrequency: 'Yearly' }, token, 201);
    const costs = (await call('/api/subscriptions/summary', 'GET', null, token, 200)).data.monthlyEquivalentTotalsByCurrency;
    assert(costs.some(x => x.currencyCode === 'TRY' && x.estimatedAmount === 199.9001) && costs.some(x => x.currencyCode === 'USD' && x.estimatedAmount === 10), 'Para birimi/aylık eşdeğer ayrımı bozuldu.');
    await call('/api/subscriptions', 'POST', { ...body, name: 'Phase5 third' }, token, 201);
    assert((await call('/api/subscriptions', 'POST', { ...body, name: 'Phase5 limit' }, token, 403)).data.code === 'entitlement_limit', 'Sunucu entitlement sınırı uygulanmadı.');
    const listed = (await call('/api/subscriptions?status=Trial&currencyCode=USD&pageSize=1', 'GET', null, token, 200)).data; assert(listed.totalCount === 1 && listed.items.length === 1, 'Sunucu filtreleri eşleşmedi.');
    const other = await account(); const otherToken = other.auth.accessToken;
    await call(`/api/subscriptions/${record.id}`, 'GET', null, otherToken, 404);
    await call(`/api/subscriptions/${record.id}`, 'PUT', { ...body, rowVersion: record.rowVersion }, otherToken, 404);
    await call(`/api/subscriptions/${record.id}/cancel`, 'POST', { accessEndsOn: day(today, 10), rowVersion: record.rowVersion }, otherToken, 404);
    await call(`/api/subscriptions/${record.id}`, 'DELETE', null, otherToken, 404, record.rowVersion);
    const oldVersion = record.rowVersion;
    const updated = await call(`/api/subscriptions/${record.id}`, 'PUT', { ...body, amount: '999999999999999.9999', rowVersion: oldVersion }, token, 200); record = updated.data;
    assert(updated.raw.includes('999999999999999.9999') && record.rowVersion !== oldVersion, 'Decimal/update version kaybı.');
    await call(`/api/subscriptions/${record.id}`, 'PUT', { ...body, rowVersion: oldVersion }, token, 409);
    const cancelled = (await call(`/api/subscriptions/${record.id}/cancel`, 'POST', { accessEndsOn: day(today, 10), rowVersion: record.rowVersion }, token, 200)).data;
    assert(cancelled.status === 'Cancelled' && !cancelled.autoRenew && cancelled.nextBillingDate === null && cancelled.accessEndsOn === day(today, 10), 'İptal/erişim semantiği eşleşmedi.');
    await call(`/api/subscriptions/${record.id}/cancel`, 'POST', { accessEndsOn: day(today, 10), rowVersion: record.rowVersion }, token, 409);
    const plan = (await call(`/api/payments/${record.paymentPlanId}`, 'GET', null, token, 200)).data; assert(!plan.isActive, 'İptal sonrası plan aktif.');
    assert(!(await call('/api/subscriptions/trials/expiring?withinDays=7', 'GET', null, token, 200)).data.some(x => x.subscriptionId === record.id), 'İptal edilen trial listeden çıkmadı.');
    await call(`/api/subscriptions/${record.id}`, 'DELETE', null, token, 204, cancelled.rowVersion);
    await call(`/api/subscriptions/${record.id}`, 'GET', null, token, 404);
    console.log(`Gerçek abonelik smoke: ${checks} HTTP kontrolü başarılı.`);
  } finally {
    let failed = false;
    for (const entry of accounts.reverse()) {
      if (!entry.id) continue;
      try {
        if (!entry.auth) entry.auth = (await call('/api/auth/login', 'POST', { email: entry.body.email, password: entry.body.password }, null, 200)).data;
        const me = (await call('/api/users/me', 'GET', null, entry.auth.accessToken, 200)).data; assert(me.id === entry.id, 'Cleanup sahipliği eşleşmedi.');
        await call('/api/users/me', 'DELETE', { currentPassword: entry.body.password }, entry.auth.accessToken, 204);
      } catch { failed = true; console.error('Oluşturulan subscription-smoke-* hesabının temizliği doğrulanamadı.'); }
    }
    if (failed) throw new Error('Smoke cleanup tamamlanamadı.');
    console.log('Yalnızca bu testin oluşturduğu hesaplar ve ilişkili kayıtları temizlendi.');
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
