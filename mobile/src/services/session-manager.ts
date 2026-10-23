import { CanceledError } from 'axios';
import type { UserResponse, AuthResponse } from '../types/api';
import type { AuthApi, LoginRequest } from './auth-api';
import { ApiError } from './api-error';
import { storedSessionSchema, type StoredSession, type SessionStorage } from './session-model';

export type SessionStatus = 'restoring' | 'anonymous' | 'authenticating' | 'authenticated';
export interface SessionSnapshot { status: SessionStatus; user: UserResponse | null; notice: string | null }
export class SessionManager {
  private tokens: StoredSession | null = null;
  private epoch = 0;
  private writes: Promise<unknown> = Promise.resolve();
  private refreshing?: { epoch: number; promise: Promise<void> };
  private restoring?: Promise<void>;
  private snapshot: SessionSnapshot = { status: 'restoring', user: null, notice: null };
  constructor(private readonly deps: { api: AuthApi; storage: SessionStorage; getUser: () => Promise<UserResponse>; clearCache: () => void; publish: (state: SessionSnapshot) => void; now?: () => number }) {}
  getSnapshot = () => this.snapshot;
  credential = () => this.tokens ? { accessToken: this.tokens.accessToken, epoch: this.epoch } : null;
  async ensureFreshAccess() {
    if (!this.tokens) throw new ApiError('Yeniden giriş yap.', 401);
    if (Date.parse(this.tokens.accessTokenExpiresAtUtc) <= this.now() + 30000) await this.refresh();
  }
  private now = () => this.deps.now?.() ?? Date.now();
  private publish(state: SessionSnapshot) { this.snapshot = state; this.deps.publish(state); }
  private check(epoch: number) { if (this.epoch !== epoch) throw new CanceledError('Oturum değişti.'); }
  private persist(action: () => Promise<void>) { const next = this.writes.then(action); this.writes = next.catch(() => {}); return next; }
  async invalidate(message = 'Oturumun sona erdi. Yeniden giriş yap.') {
    const epoch = ++this.epoch; this.tokens = null; this.deps.clearCache();
    this.publish({ status: 'anonymous', user: null, notice: message });
    try { await this.persist(() => this.deps.storage.clear()); }
    catch { if (this.epoch === epoch) this.publish({ status: 'anonymous', user: null, notice: 'Cihazdaki oturum kaydı temizlenemedi. Uygulamayı kapatmadan önce çıkışı tekrar dene.' }); }
  }
  private async accept(response: AuthResponse, epoch: number) {
    this.check(epoch);
    const tokens = storedSessionSchema.safeParse(response);
    if (!tokens.success || response.tokenType !== 'Bearer') throw new ApiError('Sunucudan geçerli bir oturum alınamadı.', undefined, undefined, undefined, 'session');
    await this.persist(async () => { this.check(epoch); await this.deps.storage.write(tokens.data); });
    this.check(epoch); this.tokens = tokens.data;
  }
  async login(request: LoginRequest) {
    if (this.snapshot.status === 'authenticating') throw new ApiError('Giriş işlemi sürüyor.');
    const epoch = ++this.epoch; this.tokens = null; this.deps.clearCache();
    this.publish({ status: 'authenticating', user: null, notice: null });
    try {
      await this.persist(() => this.deps.storage.clear());
      const response = await this.deps.api.login(request);
      try { await this.accept(response, epoch); }
      catch (error) { void this.deps.api.logout(response.refreshToken).catch(() => {}); throw error; }
      const user = await this.deps.getUser(); this.check(epoch);
      this.publish({ status: 'authenticated', user, notice: null });
    } catch (error) { if (this.epoch === epoch) await this.invalidate(''); throw error; }
  }
  restore() {
    if (!this.restoring) this.restoring = this.restoreOnce();
    return this.restoring;
  }
  private async restoreOnce() {
    const epoch = this.epoch;
    try {
      const tokens = await this.deps.storage.read(); this.check(epoch);
      if (!tokens) { this.publish({ status: 'anonymous', user: null, notice: null }); return; }
      this.tokens = tokens;
      if (Date.parse(tokens.refreshTokenExpiresAtUtc) <= this.now()) { await this.invalidate(); return; }
      if (Date.parse(tokens.accessTokenExpiresAtUtc) <= this.now() + 30000) await this.refresh();
      const user = await this.deps.getUser(); this.check(epoch);
      this.publish({ status: 'authenticated', user, notice: null });
    } catch { if (this.epoch === epoch) await this.invalidate('Oturum doğrulanamadı. Bağlantını kontrol edip yeniden giriş yap.'); }
  }
  refresh(): Promise<void> {
    const epoch = this.epoch;
    if (this.refreshing?.epoch === epoch) return this.refreshing.promise;
    const tokens = this.tokens;
    if (!tokens) return Promise.reject(new ApiError('Oturum bulunamadı.', 401, undefined, undefined, 'session'));
    const promise = (async () => {
      try {
        if (Date.parse(tokens.refreshTokenExpiresAtUtc) <= this.now()) throw new ApiError('Oturum sona erdi.', 401);
        // Never retry rotation, including timeout: the server may already have consumed it.
        await this.accept(await this.deps.api.refresh(tokens.refreshToken), epoch);
      } catch (error) {
        if (this.epoch === epoch) await this.invalidate();
        // Revoking the old family member also revokes a child issued before a lost response.
        void this.deps.api.logout(tokens.refreshToken).catch(() => {});
        throw error;
      }
    })();
    this.refreshing = { epoch, promise };
    void promise.finally(() => { if (this.refreshing?.promise === promise) this.refreshing = undefined; }).catch(() => {});
    return promise;
  }
  async logout() {
    const token = this.tokens?.refreshToken;
    await this.invalidate('Çıkış yapıldı.');
    if (token) {
      try { await this.deps.api.logout(token); }
      catch { if (this.snapshot.status === 'anonymous') this.publish({ ...this.snapshot, notice: 'Bu cihazdan çıkış yapıldı; sunucu iptali doğrulanamadı. İnternet bağlantısı olmadan sunucu oturumu süresi dolana kadar geçerli kalabilir.' }); }
    }
  }
}
