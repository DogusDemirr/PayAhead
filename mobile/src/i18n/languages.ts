export const languages = [
  { code: 'tr', name: 'Türkçe', flag: '🇹🇷', locale: 'tr-TR', rtl: false },
  { code: 'en', name: 'English', flag: '🌐', locale: 'en-US', rtl: false },
  { code: 'de', name: 'Deutsch', flag: '🇩🇪', locale: 'de-DE', rtl: false },
  { code: 'fr', name: 'Français', flag: '🇫🇷', locale: 'fr-FR', rtl: false },
  { code: 'es', name: 'Español', flag: '🇪🇸', locale: 'es-ES', rtl: false },
  { code: 'it', name: 'Italiano', flag: '🇮🇹', locale: 'it-IT', rtl: false },
  { code: 'pt-BR', name: 'Português (Brasil)', flag: '🇧🇷', locale: 'pt-BR', rtl: false },
  { code: 'nl', name: 'Nederlands', flag: '🇳🇱', locale: 'nl-NL', rtl: false },
  { code: 'ru', name: 'Русский', flag: '🇷🇺', locale: 'ru-RU', rtl: false },
  { code: 'ar', name: 'العربية', flag: '🌐', locale: 'ar', rtl: true },
  { code: 'zh-Hans', name: '简体中文', flag: '🌐', locale: 'zh-Hans', rtl: false },
  { code: 'ja', name: '日本語', flag: '🇯🇵', locale: 'ja-JP', rtl: false },
  { code: 'ko', name: '한국어', flag: '🇰🇷', locale: 'ko-KR', rtl: false },
  { code: 'hi', name: 'हिन्दी', flag: '🇮🇳', locale: 'hi-IN', rtl: false },
] as const;
export type Language = typeof languages[number]['code'];
export function isLanguage(value: unknown): value is Language { return languages.some(l => l.code === value); }
export function resolveLanguage(value?: string | null): Language {
  if (!value) return 'en';
  const exact = languages.find(l => l.code.toLowerCase() === value.toLowerCase()); if (exact) return exact.code;
  const prefix = value.toLowerCase().split('-')[0]; if (prefix === 'pt') return 'pt-BR'; if (prefix === 'zh') return /hant|tw|hk|mo/i.test(value) ? 'en' : 'zh-Hans';
  return languages.find(l => l.code === prefix)?.code ?? 'en';
}
