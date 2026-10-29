const { registrationLegal } = require('./registration-fixture.cjs');
// Explicit real-backend check. No JWT, password or response body is printed.
const { randomUUID, randomBytes } = require('node:crypto');
async function main() {
  const base = new URL(process.env.EXPO_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5080');
  const privateHost = /^(localhost|127\.0\.0\.1|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[01])\.\d+\.\d+)$/.test(base.hostname);
  if (!privateHost || !['https:', 'http:'].includes(base.protocol) || base.username || base.password || base.pathname !== '/' || base.search || base.hash)
    throw new Error('Smoke test yalnızca yerel/LAN geliştirme API kök adresine izin verir.');
  const account = { email: `mobile-smoke-${randomUUID()}@example.invalid`, displayName: 'Mobile smoke disposable', password: randomBytes(24).toString('base64url') };
  let auth; let createdId; let cleaned = false;
  async function call(path, method, body, token, expected) {
    let result;
    try { result = await fetch(new URL(path, base), { method, redirect: 'error', signal: AbortSignal.timeout(15000), headers: { Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body ? JSON.stringify(body) : undefined }); }
    catch { throw new Error(`${method} ${path}: backend erişilemiyor veya zaman aşımı.`); }
    if (result.status !== expected) throw new Error(`${method} ${path}: beklenen ${expected}, alınan ${result.status}.`);
    console.log(`${method} ${path}: ${result.status}`);
    return result.status === 204 ? null : result.json();
  }
  try {
    const registered = await call('/api/auth/register', 'POST', { ...account, ...registrationLegal() }, null, 201); createdId = registered.id;
    if ('accessToken' in registered) throw new Error('Register beklenmedik oturum döndürdü.');
    auth = await call('/api/auth/login', 'POST', { email: account.email, password: account.password }, null, 200);
    const me = await call('/api/users/me', 'GET', null, auth.accessToken, 200);
    if (me.id !== createdId || me.email !== account.email) throw new Error('Current user sözleşmesi eşleşmedi.');
    const previous = auth.refreshToken;
    auth = await call('/api/auth/refresh', 'POST', { refreshToken: previous }, null, 200);
    if (previous === auth.refreshToken) throw new Error('Refresh token rotate edilmedi.');
    await call('/api/auth/logout', 'POST', { refreshToken: auth.refreshToken }, null, 204);
    await call('/api/auth/refresh', 'POST', { refreshToken: auth.refreshToken }, null, 401);
    // Backend logout revokes the refresh family; access JWT is valid until expiry.
    await call('/api/users/me', 'GET', null, auth.accessToken, 200);
    await call('/api/users/me', 'DELETE', { currentPassword: account.password }, auth.accessToken, 204); cleaned = true;
    console.log('Gerçek backend auth smoke: başarılı; yalnızca oluşturulan test hesabı temizlendi.');
  } finally {
    if (createdId && !cleaned) {
      try {
        if (!auth) auth = await call('/api/auth/login', 'POST', { email: account.email, password: account.password }, null, 200);
        const me = await call('/api/users/me', 'GET', null, auth.accessToken, 200);
        if (me.id === createdId) { await call('/api/users/me', 'DELETE', { currentPassword: account.password }, auth.accessToken, 204); cleaned = true; }
      } catch { console.error('Test hesabı temizliği doğrulanamadı; rastgele mobile-smoke-* hesabı geliştirme ortamında kalmış olabilir.'); }
    }
  }
}
main().catch((error) => { console.error(error.message); process.exitCode = 1; });
