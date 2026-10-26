import './polyfills';
import { createInstance } from 'i18next';
import { initReactI18next } from 'react-i18next';
import { getLocales } from 'expo-localization';
import { create } from 'zustand';
import { I18nManager, Platform } from 'react-native';
import { languages, resolveLanguage, type Language } from './languages';
import { LanguagePreference } from './preference';
import { languageStorage } from './storage';
import resources from './resources.json';
import pluralRoots from './plural-roots.json';
import sourceManifest from './source-manifest.json';

const i18next = createInstance();
let deviceLanguage: string | undefined;
try { deviceLanguage = getLocales()[0]?.languageTag; } catch { /* unsupported native host: English */ }
const initial = resolveLanguage(deviceLanguage);
export const useLanguageStore = create<{ language: Language; ready: boolean; syncPending: boolean; storageFailed: boolean }>(() => ({ language: initial, ready: false, syncPending: false, storageFailed: false }));
void i18next.use(initReactI18next).init({ resources, lng: initial, fallbackLng: 'en', defaultNS: 'common', ns: ['common', 'auth', 'dashboard', 'payments', 'subscriptions', 'catalog', 'calendar', 'analytics', 'reminders', 'settings', 'premium', 'errors', 'validation'], keySeparator: false, initAsync: false, interpolation: { escapeValue: false }, returnNull: false, saveMissing: __DEV__, missingKeyHandler: (_languages, namespace, key) => { if (__DEV__) console.warn('i18n missing key', namespace + ':' + key); } });
export const languagePreference = new LanguagePreference(deviceLanguage, languageStorage, language => {
  void i18next.changeLanguage(language);
  useLanguageStore.setState({ language });
});
export function useLocaleRevision() { return useLanguageStore(s => s.language); }
export function currentLanguage() { return useLanguageStore.getState().language; }
export function currentLocale() { return languages.find(x => x.code === currentLanguage())!.locale; }
export function isRTL() { return languages.find(x => x.code === currentLanguage())!.rtl; }
export function t(key: string, values?: Record<string, unknown>): string { const count = pluralRoots.includes(key.split(':')[1]) && typeof values?.value0 === 'number' ? values.value0 : undefined; return String(i18next.t(key, { ...values, ...(count === undefined ? {} : { count }) })); }
export function errorMessage(value?: string, preserveUnknown = false) {
  if (!value) return undefined;
  if (/^[a-z]+:[\w.]+$/.test(value) && i18next.exists(value)) return t(value);
  const source = sourceManifest.find(row => row.text === value);
  if (source) return t(source.key);
  for (const data of Object.values(resources) as Record<string, Record<string, string>>[]) for (const [namespace, rows] of Object.entries(data)) for (const [key, text] of Object.entries(rows)) if (text === value) return t(namespace + ':' + key);
  return preserveUnknown ? value : t('validation:invalidValue');
}
export async function chooseLanguage(language: Language) {
  useLanguageStore.setState({ storageFailed: false, syncPending: true });
  try { await languagePreference.select(language); }
  catch { useLanguageStore.setState({ storageFailed: true }); }
  if (Platform.OS !== 'web') { I18nManager.allowRTL(true); I18nManager.forceRTL(languages.find(x => x.code === language)!.rtl); }
}
export async function restoreLanguage() { await languagePreference.restore(); useLanguageStore.setState({ ready: true }); }
export { i18next };
