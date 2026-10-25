// Local development only. No real user credentials, no provider call, own disposable accounts only.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  if (!/^(localhost|127\.0\.0\.1|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[01])\.\d+\.\d+)$/.test(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw new Error('Yalnız yerel/LAN API kökü kabul edilir.');
  let checks = 0; const accounts = []; const assert = (ok, message) => { if (!ok) throw new Error(message); };
  async function call(path, method, body, token, expected, version) {
    const json = body && 'amount' in body ? (() => { const { amount, ...rest } = body; return JSON.stringify(rest).slice(0, -1) + `,"amount":${amount}}`; })() : body ? JSON.stringify(body) : undefined;
    let response; try { response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { Accept: 'application/json', ...(json ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(version ? { 'If-Match': `"${version}"` } : {}) }, body: json }); } catch { throw new Error('Yerel API bağlantısı kurulamadı.'); }
    const label = `${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}`; assert(response.status === expected, `${label}: beklenen ${expected}, alınan ${response.status}`); checks++; console.log(`${label}: ${response.status}`); const raw = await response.text(); return { raw, data: raw ? JSON.parse(raw) : null };
  }
  async function account() { const body = { email: `catalog-smoke-${randomUUID()}@example.invalid`, displayName: 'Disposable catalog smoke', password: randomBytes(24).toString('base64url') }; const entry = { body, id: null, auth: null }; accounts.push(entry); entry.id = (await call('/api/auth/register', 'POST', body, null, 201)).data.id; entry.auth = (await call('/api/auth/login', 'POST', { email: body.email, password: body.password }, null, 200)).data; return entry; }
  try {
    await call('/api/catalog/categories', 'GET', null, null, 401); const owner = await account(); const token = owner.auth.accessToken;
    const cats = (await call('/api/catalog/categories', 'GET', null, token, 200)).data;
    const category = code => { const value = cats.find(x => x.code === code); assert(value, 'Kategori bulunamadı.'); return value.id; };
    assert(cats.length === 116 && cats.some(x => x.parentId === category('transport.fuel') && x.code === 'transport.petrol'), 'Hiyerarşi eşleşmedi.');
    assert((await call('/api/catalog/categories?search=arac', 'GET', null, token, 200)).data.some(x => x.code === 'transport'), 'Türkçe arama eşleşmedi.');
    const services = (await call('/api/catalog/services?pageSize=100', 'GET', null, token, 200)).data; assert(services.totalCount === 102 && !services.items.some(x => ['apple-tv', 'bp'].includes(x.code)), 'Aktif katalog filtresi eşleşmedi.');
    const netflix = services.items.find(x => x.code === 'netflix'), shell = services.items.find(x => x.code === 'shell'); assert(netflix && shell && shell.applicableTo === 'Payment', 'Hizmetler eşleşmedi.');
    const subscriptionCategories = (await call('/api/catalog/categories?applicableTo=Subscription', 'GET', null, token, 200)).data; assert(subscriptionCategories.length === 50 && !subscriptionCategories.some(c => c.code.startsWith('transport')), 'Abonelik kategori kapsamı eşleşmedi.');
    assert((await call('/api/catalog/categories?applicableTo=Payment', 'GET', null, token, 200)).data.length === 115, 'Ödeme kategori kapsamı eşleşmedi.');
    assert((await call(`/api/catalog/services?categoryId=${category('transport.fuel')}&applicableTo=Payment`, 'GET', null, token, 200)).data.items.some(x => x.id === shell.id && x.categoryIds.includes(category('transport.fuel'))), 'Yakıt → Shell bağlantısı eksik.');
    assert((await call(`/api/catalog/services?categoryId=${category('transport.service')}`, 'GET', null, token, 200)).data.items.some(x => x.displayName === 'Bosch Car Service'), 'Bakım ve Tamir bağlantısı eksik.');
    const papers = (await call(`/api/catalog/services?categoryId=${category('news.digital-newspaper')}&applicableTo=Subscription`, 'GET', null, token, 200)).data.items; const nyt = papers.find(x => x.code === 'new-york-times'); assert(nyt, 'Dijital gazete hizmetleri eksik.');
    assert((await call('/api/catalog/services?search=ARA%C3%87&pageSize=100', 'GET', null, token, 200)).data.items.some(x => x.id === shell.id), 'Hiyerarşik Türkçe arama eşleşmedi.');
    assert((await call('/api/catalog/services?pageSize=100&pageNumber=2', 'GET', null, token, 200)).data.items.length === 2, 'Katalog sayfalama eşleşmedi.');
    await call(`/api/catalog/services/${netflix.id}`, 'GET', null, token, 200); await call('/api/catalog/services?pageSize=101', 'GET', null, token, 400);
    assert((await call(`/api/catalog/services?categoryId=${category('transport.diesel')}`, 'GET', null, token, 200)).data.items.some(x => x.id === shell.id), 'Çoklu kategori bağlantısı eşleşmedi.');
    const today = (await call('/api/dashboard/summary', 'GET', null, token, 200)).data.context.today;
    const body = { name: 'Private Shell label', description: null, amount: '12.3456', currencyCode: 'TRY', category: 'Benzin', frequency: 'Once', interval: 1, startDate: today, endDate: null, anchorDay: null, isActive: true, catalogCategoryId: category('transport.petrol'), catalogServiceId: shell.id };
    const payment = (await call('/api/payments', 'POST', body, token, 201)).data;
    const legacyBody = { ...body, name: 'Legacy private label', isActive: false }; delete legacyBody.catalogCategoryId; delete legacyBody.catalogServiceId;
    const legacy = (await call('/api/payments', 'POST', legacyBody, token, 201)).data; assert(legacy.catalogCategoryId === null && legacy.catalogServiceId === null && legacy.name === legacyBody.name, 'Eski sözleşme korunmadı.');
    const request = { name: 'Private Netflix label', description: null, providerName: 'Personal provider label', amount: '199.9001', currencyCode: 'TRY', billingFrequency: 'Monthly', billingInterval: 1, startDate: today, firstBillingDate: today, trialStartDate: null, trialEndDate: null, status: 'Active', autoRenew: true, accessEndsOn: null, enableTrialReminders: false, anchorDay: null, catalogCategoryId: category('video'), catalogServiceId: netflix.id };
    const subscription = (await call('/api/subscriptions', 'POST', request, token, 201)).data;
    assert((await call('/api/payments?pageSize=100', 'GET', null, token, 200)).data.totalCount === 3, 'Linked plan çift oluşturuldu.');
    const prefix = today.slice(0, 7); const [year, month] = prefix.split('-').map(Number); const last = new Date(Date.UTC(year, month, 0)).getUTCDate();
    const dashboard = await call(`/api/dashboard/summary?fromDate=${prefix}-01&toDate=${prefix}-${last}`, 'GET', null, token, 200); assert(dashboard.raw.includes('212.2457'), 'Finansal toplam/decimal korunmadı.');
    const calendar = (await call(`/api/calendar?year=${year}&month=${month}`, 'GET', null, token, 200)).data; const rows = calendar.days.flatMap(x => x.payments); assert(rows.length === 2 && rows.filter(x => x.paymentPlanId === subscription.paymentPlanId).length === 1 && rows.every(x => x.catalogCategoryId), 'Takvim/metadata tek kaynak eşleşmedi.');
    const oldUpdate = { ...legacyBody, name: payment.name, isActive: true, rowVersion: payment.rowVersion }; const changed = (await call(`/api/payments/${payment.id}`, 'PUT', oldUpdate, token, 200)).data; assert(changed.catalogServiceId === shell.id, 'Eski PUT bağlantıyı kaybetti.'); await call(`/api/payments/${payment.id}`, 'PUT', oldUpdate, token, 409);
    const subUpdate = { ...request, rowVersion: subscription.rowVersion }; delete subUpdate.catalogCategoryId; delete subUpdate.catalogServiceId; const subChanged = (await call(`/api/subscriptions/${subscription.id}`, 'PUT', subUpdate, token, 200)).data; assert(subChanged.catalogServiceId === netflix.id && subChanged.name === request.name, 'Abonelik bağlantı/özel isim korunmadı.');
    await call('/api/payments', 'POST', { ...body, catalogCategoryId: category('video') }, token, 400); await call('/api/subscriptions', 'POST', { ...request, catalogCategoryId: category('transport.petrol'), catalogServiceId: shell.id }, token, 400);
    const other = await account(); await call(`/api/payments/${payment.id}`, 'GET', null, other.auth.accessToken, 404); await call(`/api/subscriptions/${subscription.id}`, 'GET', null, other.auth.accessToken, 404);
    assert((await call('/api/catalog/services?pageSize=100', 'GET', null, other.auth.accessToken, 200)).data.totalCount === 102, 'Özel isim ortak kataloğa eklendi.');
    const newspaper = (await call('/api/subscriptions', 'POST', { ...request, name: 'Disposable newspaper', providerName: nyt.displayName, catalogCategoryId: category('news.digital-newspaper'), catalogServiceId: nyt.id }, token, 201)).data;
    assert(newspaper.catalogServiceId === nyt.id && newspaper.catalogCategoryId === category('news.digital-newspaper'), 'Gazete aboneliği bağlantısı eşleşmedi.');
    await call(`/api/subscriptions/${newspaper.id}`, 'DELETE', null, token, 204, newspaper.rowVersion);
    await call(`/api/subscriptions/${subscription.id}`, 'DELETE', null, token, 204, subChanged.rowVersion); await call(`/api/payments/${payment.id}`, 'DELETE', null, token, 204, changed.rowVersion);
    console.log(`Gerçek katalog smoke: ${checks} HTTP kontrolü başarılı.`);
  } finally {
    let failed = false;
    for (const entry of accounts.reverse()) { if (!entry.id) continue; try { if (!entry.auth) entry.auth = (await call('/api/auth/login', 'POST', { email: entry.body.email, password: entry.body.password }, null, 200)).data; assert((await call('/api/users/me', 'GET', null, entry.auth.accessToken, 200)).data.id === entry.id, 'Cleanup sahipliği'); await call('/api/users/me', 'DELETE', { currentPassword: entry.body.password }, entry.auth.accessToken, 204); } catch { failed = true; console.error('Testin oluşturduğu catalog-smoke-* hesabının temizliği doğrulanamadı.'); } }
    if (failed) throw new Error('Smoke cleanup tamamlanamadı.'); console.log('Yalnızca testin kendi hesapları ve ilişkili kayıtları temizlendi.');
  }
}
main().catch(e => { console.error(e.message); process.exitCode = 1; });
