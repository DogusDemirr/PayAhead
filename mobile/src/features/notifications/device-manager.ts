import { z } from 'zod';
export const bindingSchema = z.object({ userId: z.string(), id: z.uuid() });
export type DeviceBinding = z.infer<typeof bindingSchema>;
export interface DeviceIdentity { userId: string; epoch: number; accessToken: string }
export interface DeviceTransport { register(token: string, id?: string): Promise<{ id: string }>; remove(id: string): Promise<void> }
export interface DeviceManagerDeps {
  identity(): DeviceIdentity | null;
  prepare(): Promise<void>;
  storage: { read(): Promise<DeviceBinding | null>; write(value: DeviceBinding): Promise<void>; clear(): Promise<void> };
  transport(identity: DeviceIdentity): DeviceTransport;
  token(): Promise<string>;
}
// Serializes rotation, logout and account changes. Captured bearer is memory-only and
// can only clean up its own registration; a new user's bearer is never substituted.
export class DeviceManager {
  private queue: Promise<unknown> = Promise.resolve();
  constructor(private deps: DeviceManagerDeps) {}
  private run<T>(action: () => Promise<T>) { const next = this.queue.then(action); this.queue = next.catch(() => {}); return next; }
  async sync() {
    const requested = this.deps.identity();
    if (!requested) throw new Error('Oturum değişti.');
    // Refresh can invalidate the session and retire a device. Do not hold this
    // serial queue while waiting for refresh, or cleanup would wait on itself.
    await this.deps.prepare();
    return this.run(async () => {
      if (!requested || this.deps.identity()?.epoch !== requested.epoch) throw new Error('Oturum değişti.');
      const identity = this.deps.identity();
      if (!identity || identity.epoch !== requested.epoch) throw new Error('Oturum değişti.');
      const binding = await this.deps.storage.read();
      if (binding && binding.userId !== identity.userId) throw new Error('Önceki hesabın cihaz kaydı temizlenemedi. O hesapla giriş yapıp cihaz kaydını kaldır veya Android ayarlarından bildirimleri kapat.');
      const token = await this.deps.token();
      if (this.deps.identity()?.epoch !== identity.epoch) throw new Error('Oturum değişti.');
      const api = this.deps.transport(identity); const device = await api.register(token, binding?.id);
      const saved = { userId: identity.userId, id: device.id };
      // Retain the identifier before cleanup, so an offline cleanup cannot be forgotten.
      try { await this.deps.storage.write(saved); }
      catch (error) { await api.remove(device.id); throw error; }
      if (this.deps.identity()?.epoch !== identity.epoch) { await api.remove(device.id); await this.deps.storage.clear(); throw new Error('Oturum değişti.'); }
      return device.id;
    });
  }
  retire(identity: DeviceIdentity) {
    return this.run(async () => {
      const binding = await this.deps.storage.read();
      if (!binding || binding.userId !== identity.userId) return;
      await this.deps.transport(identity).remove(binding.id); await this.deps.storage.clear();
    });
  }
}
