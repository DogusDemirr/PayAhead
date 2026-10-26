import type { Language } from './languages';
import { isLanguage, resolveLanguage } from './languages';

export interface LanguageStorage { read(): Promise<unknown>; write(language: Language): Promise<void> }
export class LanguagePreference {
  manual: Language | null = null;
  language: Language;
  revision = 0;
  private writes: Promise<void> = Promise.resolve();
  constructor(private device: string | undefined, private storage: LanguageStorage, private changed: (language: Language) => void) { this.language = resolveLanguage(device); }
  private apply(language: Language) { this.language = language; this.changed(language); }
  async restore() {
    const revision = this.revision;
    let value: unknown; try { value = await this.storage.read(); } catch { /* device fallback; no private data in logs */ }
    if (revision !== this.revision) return;
    if (isLanguage(value)) this.manual = value;
    this.apply(this.manual ?? resolveLanguage(this.device));
  }
  async select(language: Language) {
    this.revision++; this.manual = language; this.apply(language);
    this.writes = this.writes.catch(() => undefined).then(() => this.storage.write(language));
    await this.writes;
  }
  accountChanged() { this.revision++; this.apply(this.manual ?? resolveLanguage(this.device)); }
  acceptAccount(language: unknown, revision: number) { if (revision === this.revision && !this.manual && isLanguage(language)) this.apply(language); }
}
