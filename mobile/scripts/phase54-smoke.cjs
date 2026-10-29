// Yalnız loopback Development API; sadece kendi geçici hesabını temizler.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  if (!['localhost', '127.0.0.1'].includes(base.hostname) || !['http:', 'https:'].includes(base.protocol) || base.pathname !== '/' || base.username || base.password || base.search || base.hash) throw Error('Loopback Development root required');
  const email = `phase54-${randomUUID()}@example.invalid`; let password = randomBytes(24).toString('base64url'); let token, id, checks = 0;
  const version = '2026-10-29-draft-1';
  const assert = (value, message) => { if (!value) throw Error(message); };
  async function call(path, method = 'GET', body, expected = 200) {
    const response = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(20000), headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body ? { 'Content-Type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined });
    assert(response.status === expected, `${method} ${path}: expected ${expected}, got ${response.status}`); checks++;
    const raw = await response.text(); return raw ? JSON.parse(raw) : null;
  }
  async function login() { token = (await call('/api/auth/login', 'POST', { email, password })).accessToken; }
  try {
    await call('/health/ready'); const manifest = await call('/api/legal'); assert(manifest.version === version && manifest.isDraft && !manifest.requiresTermsAcceptance, 'Draft manifest mismatch');
    await call('/api/users/me/security', 'GET', null, 401);
    id = (await call('/api/auth/register', 'POST', { email, password, displayName: 'Disposable Phase54', legalVersion: version }, 201)).id;
    await login(); const first = await call('/api/users/me/security'); assert(first.remainingChanges === 3 && first.activeSessions === 1, 'Initial summary mismatch');
    const exported = await call('/api/users/me/export'); assert(exported.legalNotice.noticeVersion === version && exported.legalNotice.presentedAtUtc, 'Notice receipt missing');
    assert(!JSON.stringify(exported).match(/passwordHash|tokenHash|passwordHistory/i), 'Export included secret fields');
    const initial = password;
    for (let i = 1; i <= 3; i++) {
      const next = randomBytes(24).toString('base64url');
      await call('/api/users/me/change-password', 'POST', { currentPassword: password, newPassword: next, confirmation: next }, 204); password = next;
      await call('/api/users/me/security', 'GET', null, 401); await login();
      assert((await call('/api/users/me/security')).remainingChanges === 3 - i, 'Remaining allowance mismatch');
    }
    const fourth = randomBytes(24).toString('base64url');
    assert((await call('/api/users/me/change-password', 'POST', { currentPassword: password, newPassword: fourth, confirmation: fourth }, 409)).code === 'password_change_limit', 'Limit code mismatch');
    assert((await call('/api/users/me/change-password', 'POST', { currentPassword: password, newPassword: initial, confirmation: initial }, 400)).code === 'password_reused', 'History code mismatch');
    const final = await call('/api/users/me/security'); assert(final.remainingChanges === 0 && final.nextAvailableAtUtc, 'Next availability missing');
    console.log(`Phase 5.4 gerçek Development HTTP: ${checks} kontrol geçti; yanıt gövdeleri, parola ve token yazdırılmadı.`);
  } finally {
    if (id) { await login(); assert((await call('/api/users/me')).id === id, 'Cleanup ownership mismatch'); await call('/api/users/me', 'DELETE', { currentPassword: password }, 204); console.log('Yalnız geçici Phase54 test hesabı temizlendi.'); }
  }
}
main().catch(() => { console.error('Phase 5.4 HTTP testi başarısız; hassas veri yazdırılmadı. HTTP durumunu API logundan inceleyin.'); process.exitCode = 1; });
