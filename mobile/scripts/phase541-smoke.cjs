const { randomUUID, randomBytes } = require('node:crypto');
const { registrationLegal } = require('./registration-fixture.cjs');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  if (!['localhost', '127.0.0.1'].includes(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw Error('Loopback Development required');
  const account = { email: `phase541-${randomUUID()}@example.invalid`, password: randomBytes(24).toString('base64url'), displayName: 'Disposable Phase541' };
  let id, token, checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); };
  async function call(path, method = 'GET', body, expected = 200) {
    const response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(20000), headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body ? { 'Content-Type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined });
    const raw = await response.text(); const json = raw ? JSON.parse(raw) : null;
    if (path === '/api/auth/register' && response.status === 201) id = json.id;
    assert(response.status === expected, `${method} ${path}: expected ${expected}, got ${response.status}`); checks++; return json;
  }
  try {
    await call('/health/ready'); const manifest = await call('/api/legal');
    assert(manifest.version === registrationLegal().legalVersion && manifest.isDraft && manifest.requiresTermsAcceptance && manifest.requiresNoticeAcknowledgment && manifest.requiresPrivacyAcknowledgment, 'Manifest mismatch');
    await call('/api/auth/register', 'POST', account, 400);
    for (const delta of [{ noticeAcknowledged: false }, { privacyAcknowledged: false }, { termsAccepted: false }, { noticeVersion: 'stale' }, { privacyVersion: null }, { termsVersion: 'stale' }]) {
      await call('/api/auth/register', 'POST', { ...account, ...registrationLegal(), legal: { ...registrationLegal().legal, ...delta } }, 400);
    }
    await call('/api/auth/register', 'POST', { ...account, ...registrationLegal() }, 201);
    token = (await call('/api/auth/login', 'POST', { email: account.email, password: account.password })).accessToken;
    const exported = await call('/api/users/me/export'); assert(exported.registrationLegal.length === 3, 'Confirmation count mismatch');
    const kinds = { notice: 'NoticeAcknowledgment', privacy: 'PrivacyReadAcknowledgment', terms: 'TermsAcceptance' };
    for (const [document, kind] of Object.entries(kinds)) {
      const item = exported.registrationLegal.find(row => row.documentId === document);
      assert(item && item.kind === kind && item.documentVersion === manifest.version && item.confirmedAtUtc.endsWith('Z'), 'Version/type/UTC mismatch');
    }
    assert(!JSON.stringify(exported).match(/passwordHash|tokenHash|passwordHistory/i), 'Secret field in export');
    console.log(`Phase 5.4.1 gerçek Development HTTP: ${checks} kontrol geçti; üç ayrı teyit türü/sürüm/UTC kaydı doğrulandı.`);
  } finally {
    if (id) { if (!token) token = (await call('/api/auth/login', 'POST', { email: account.email, password: account.password })).accessToken; assert((await call('/api/users/me')).id === id, 'Cleanup ownership mismatch'); await call('/api/users/me', 'DELETE', { currentPassword: account.password }, 204); console.log('Yalnız geçici Phase541 test hesabı temizlendi.'); }
  }
}
main().catch(error => { if (/^(GET|POST|PUT|DELETE) \/api\//.test(error.message)) console.error(error.message); console.error('Phase 5.4.1 smoke başarısız; yanıt gövdeleri, parola ve token yazdırılmadı.'); process.exitCode = 1; });
