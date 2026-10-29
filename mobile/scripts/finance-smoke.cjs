const { registrationLegal } = require('./registration-fixture.cjs');
// Real development API only. Creates/cleans its own disposable accounts; never prints financial bodies or credentials.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  if (!/^(localhost|127\.0\.0\.1|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[01])\.\d+\.\d+)$/.test(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw new Error('Yalnızca yerel/LAN geliştirme API kökü kabul edilir.');
  const accounts = []; let checks = 0;
  function assert(condition, message) { if (!condition) throw new Error(message); }
  async function call(path, method, body, token, status, etag) {
    const json = body && 'amount' in body ? (() => { const { amount, ...rest } = body; return JSON.stringify(rest).slice(0, -1) + `,"amount":${amount}}`; })() : body ? JSON.stringify(body) : undefined;
    let response;
    try { response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { Accept: 'application/json', ...(json ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(etag ? { 'If-Match': `"${etag}"` } : {}) }, body: json }); }
    catch { throw new Error(`${method} API: bağlantı kurulamadı.`); }
    assert(response.status === status, `${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}: beklenen ${status}, alınan ${response.status}.`);
    checks++; console.log(`${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}: ${response.status}`);
    const raw = await response.text(); return { data: raw ? JSON.parse(raw) : null, raw };
  }
  async function account() {
    const record = { email: `finance-smoke-${randomUUID()}@example.invalid`, displayName: 'Disposable finance smoke', password: randomBytes(24).toString('base64url') };
    const entry = { record, id: null, auth: null }; accounts.push(entry);
    entry.id = (await call('/api/auth/register', 'POST', { ...record, ...registrationLegal() }, null, 201)).data.id;
    entry.auth = (await call('/api/auth/login', 'POST', { email: record.email, password: record.password }, null, 200)).data; return entry;
  }
  try {
    await call('/api/payments', 'GET', null, null, 401);
    const owner = await account(); const token = owner.auth.accessToken;
    const initial = (await call('/api/dashboard/summary', 'GET', null, token, 200)).data;
    const today = initial.context.today; const prefix = today.slice(0, 7); const [year, month] = prefix.split('-').map(Number);
    const last = new Date(Date.UTC(year, month, 0)).getUTCDate(); const range = `fromDate=${prefix}-01&toDate=${prefix}-${last}`;
    const ent = (await call('/api/premium/entitlements', 'GET', null, token, 200)).data;
    const limit = ent.limits.activePaymentPlans; assert(Number.isInteger(limit) && limit > 1 && limit <= 10 && !ent.isPremium, 'Smoke testi yalnızca küçük Free limitli test hesabında çalışır.');
    const plans = []; const inputs = [];
    for (let index = 0; index < limit; index++) {
      const body = { name: `Mobile phase3 ${index}`, description: 'Disposable test record', amount: index === 0 ? '999999999999999.9999' : '5.9901', currencyCode: index === 1 ? 'USD' : 'TRY', category: 'Mobile smoke', frequency: ['Monthly', 'Weekly', 'Daily', 'Once', 'Yearly'][index % 5], interval: 1, startDate: today, endDate: null, anchorDay: null, isActive: true };
      const created = await call('/api/payments', 'POST', body, token, 201); if (index === 0) assert(created.raw.includes('999999999999999.9999'), 'Decimal precision kaybı.');
      plans.push(created.data); inputs.push(body);
    }
    const rejected = await call('/api/payments', 'POST', { ...inputs[0], name: 'Limit probe' }, token, 403); assert(rejected.data.code === 'entitlement_limit', 'Entitlement kodu eşleşmedi.');
    const listed = (await call('/api/payments?pageNumber=1&pageSize=2&search=Mobile%20phase3&isActive=true', 'GET', null, token, 200)).data;
    assert(listed.totalCount === limit && listed.items.length === 2 && listed.totalPages === Math.ceil(limit / 2), 'Server pagination/filter sözleşmesi eşleşmedi.');
    const dashboard = (await call(`/api/dashboard/summary?${range}`, 'GET', null, token, 200)).data;
    assert(dashboard.activePaymentPlanCount === limit && dashboard.upcomingTotalsByCurrency.some((x) => x.currencyCode === 'TRY') && dashboard.upcomingTotalsByCurrency.some((x) => x.currencyCode === 'USD'), 'Dashboard para birimi ayrımı eşleşmedi.');
    await call(`/api/payments/upcoming?${range}&status=Pending&pageNumber=1&pageSize=3`, 'GET', null, token, 200);
    const other = await account(); await call(`/api/payments/${plans[0].id}`, 'GET', null, other.auth.accessToken, 404);
    const changed = (await call(`/api/payments/${plans[0].id}`, 'PUT', { ...inputs[0], name: 'Updated smoke', rowVersion: plans[0].rowVersion }, token, 200)).data;
    assert(changed.rowVersion !== plans[0].rowVersion, 'RowVersion değişmedi.');
    await call(`/api/payments/${plans[0].id}`, 'PUT', { ...inputs[0], rowVersion: plans[0].rowVersion }, token, 409);
    await call(`/api/payments/${plans[0].id}`, 'DELETE', null, token, 409, plans[0].rowVersion);
    await call(`/api/payments/${plans[0].id}`, 'DELETE', null, token, 204, changed.rowVersion);
    await call(`/api/payments/${plans[0].id}`, 'GET', null, token, 404);
    await call(`/api/payments/${plans[1].id}`, 'PUT', { ...inputs[1], isActive: false, rowVersion: plans[1].rowVersion }, token, 200);
    const inactive = (await call('/api/payments?isActive=false&currencyCode=USD', 'GET', null, token, 200)).data;
    assert(inactive.totalCount === 1, 'Pasifleştirme/para birimi filtresi eşleşmedi.');
    console.log(`Gerçek finans smoke: ${checks} HTTP kontrolü başarılı.`);
  } finally {
    let failed = false;
    for (const entry of accounts.reverse()) {
      if (!entry.id) continue;
      try {
        if (!entry.auth) entry.auth = (await call('/api/auth/login', 'POST', { email: entry.record.email, password: entry.record.password }, null, 200)).data;
        const me = (await call('/api/users/me', 'GET', null, entry.auth.accessToken, 200)).data; assert(me.id === entry.id, 'Cleanup sahibi eşleşmedi.');
        await call('/api/users/me', 'DELETE', { currentPassword: entry.record.password }, entry.auth.accessToken, 204);
      } catch { failed = true; console.error('Oluşturulan finance-smoke-* test hesabının temizliği doğrulanamadı.'); }
    }
    if (failed) throw new Error('Smoke cleanup tamamlanamadı.');
    console.log('Yalnızca testin oluşturduğu hesaplar ve ilişkili test kayıtları temizlendi.');
  }
}
main().catch((error) => { console.error(error.message); process.exitCode = 1; });
