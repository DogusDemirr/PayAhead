const { registrationLegal } = require('./registration-fixture.cjs');
// Only local Development API and disposable accounts; never prints credentials or private responses.
const { randomUUID, randomBytes } = require('node:crypto');
const names = require('../../src/PayAhead.Infrastructure/Localization/ServiceNames.json');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5088');
  if (!['127.0.0.1', 'localhost'].includes(base.hostname) || base.pathname !== '/' || base.username || base.password || base.search || base.hash || !['http:', 'https:'].includes(base.protocol)) throw Error('Yalnız yerel API kökü kabul edilir.');
  const assert = (ok, message) => { if (!ok) throw Error(message); }; let checks = 0, id, token;
  const account = { email: `localization-smoke-${randomUUID()}@example.invalid`, displayName: 'Disposable locale smoke', password: randomBytes(24).toString('base64url') };
  async function call(path, method = 'GET', body, expected = 200, anonymous = false) {
    let response; try { response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { 'Content-Type': 'application/json', ...(!anonymous && token ? { Authorization: `Bearer ${token}` } : {}) }, body: body ? JSON.stringify(body) : undefined }); } catch { throw Error('Yerel API bağlantısı kurulamadı.'); }
    assert(response.status === expected, `${method} ${path.split('?')[0].replace(/[0-9a-f]{8}-[0-9a-f-]{27,}/g, '{id}')}: ${response.status}, beklenen ${expected}`); checks++;
    const raw = await response.text(); return raw ? JSON.parse(raw) : null;
  }
  try {
    await call('/health/ready'); await call('/api/users/me/language', 'GET', null, 401, true);
    id = (await call('/api/auth/register', 'POST', { ...account, ...registrationLegal() }, 201)).id;
    token = (await call('/api/auth/login', 'POST', { email: account.email, password: account.password })).accessToken;
    const first = await call('/api/catalog/services?pageSize=100&locale=en'); const shell = first.items.find(x => x.code === 'shell'); const generic = first.items.find(x => x.code === 'elektrik-faturasi'); assert(shell && generic, 'Hizmetler eksik.');
    for (const locale of Object.keys(names)) {
      assert((await call('/api/users/me/language', 'PUT', { languageCode: locale })).languageCode === locale, 'Dil PUT eşleşmedi.');
      assert((await call('/api/users/me/language')).languageCode === locale, 'Dil kalıcı değil.');
      const categories = await call('/api/catalog/categories?locale=' + locale);
      assert(categories.length === 116 && categories.every(x => x.cultureCode === locale && x.displayName && !x.usedLanguageFallback), 'Kategori çevirileri eksik.');
      assert((await call(`/api/catalog/services/${shell.id}?locale=${locale}`)).displayName === 'Shell', 'Marka değişti.');
      assert((await call(`/api/catalog/services/${generic.id}?locale=${locale}`)).displayName === names[locale]['elektrik-faturasi'], 'Genel hizmet çevrilmedi.');
      console.log(`${locale}: tercih, 116 kategori, marka ve genel hizmet doğrulandı.`);
    }
    const search = await call('/api/catalog/services?locale=de&search=' + encodeURIComponent(names.de['elektrik-faturasi'])); assert(search.items.some(x => x.id === generic.id), 'Yerel hizmet araması eşleşmedi.');
    await call('/api/catalog/services/' + shell.id + '?locale=xx', 'GET', null, 400);
    await call('/api/users/me/language', 'PUT', { languageCode: 'xx' }, 400);
    await call('/api/users/me/language', 'PUT', { languageCode: 'en', userId: randomUUID() }, 400);
    console.log(`Yerelleştirme smoke: ${checks} HTTP kontrolü başarılı.`);
  } finally {
    if (id && token) {
      assert((await call('/api/users/me')).id === id, 'Cleanup sahipliği doğrulanamadı.');
      await call('/api/users/me', 'DELETE', { currentPassword: account.password }, 204);
      console.log('Yalnız testin kendi hesabı temizlendi.');
    } else if (id) throw Error('Test hesabı oluştu ancak cleanup için oturum alınamadı.');
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
