import { beforeEach, expect, jest, test } from '@jest/globals';
import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { AppInput, AppText } from '../src/components/ui';
import { LanguagePreference } from '../src/i18n/preference';
import { languages, resolveLanguage } from '../src/i18n/languages';
import { currentLanguage, i18next, isRTL, t, useLanguageStore, useLocaleRevision } from '../src/i18n/runtime';
import { canonicalAmount, financialMoney } from '../src/features/payments/money';
import { mapApiError } from '../src/services/api-error';
import resources from '../src/i18n/resources.json';
import manifest from '../src/i18n/source-manifest.json';
import { forgotSchema } from '../src/services/password-reset';
import { dateLabel } from '../src/utils/format';
import LanguageScreen from '../src/features/LanguageScreen';
import { useUiStore } from '../src/store/ui';
import { reloadAppAsync } from 'expo';
jest.mock('expo', () => ({ reloadAppAsync: jest.fn(async () => undefined) }));
jest.mock('../src/i18n/storage', () => ({ languageStorage: { read: async () => null, write: async () => undefined } }));

function select(language: typeof languages[number]['code']) { void i18next.changeLanguage(language); useLanguageStore.setState({ language }); }
beforeEach(() => select('tr'));
test.each(languages)('resolves $code and formats exact decimal without converting currency', ({ code }) => {
  select(code); expect(currentLanguage()).toBe(code); expect(isRTL()).toBe(code === 'ar');
  const result = financialMoney('999999999999999.9999', 'USD');
  const parts = new Intl.NumberFormat(languages.find(x => x.code === code)!.locale, { useGrouping: false }).format(9999);
  expect(result).toContain(parts); expect(result).not.toContain('NaN');
  expect(canonicalAmount('٩٩٩٩٩٩٩٩٩٩٩٩٩٩٩٫٩٩٩٩')).toBe('999999999999999.9999');
  expect(canonicalAmount('१२०,२५')).toBe('120.25'); expect(canonicalAmount('1,234.56')).toBeNull();
});
test.each(languages)('$code has every screen key, CLDR plural and runtime validation', ({ code, locale }) => {
  select(code);
  const bundle = resources[code] as Record<string, Record<string, string>>;
  for (const row of manifest) { const [ns, key] = row.key.split(':'); expect(bundle[ns][key]?.trim()).toBeTruthy(); }
  for (const count of [0, 1, 2, 3, 11, 100, 1000000]) {
    expect(t('common:paymentCount', { count })).not.toMatch(/{{|paymentCount/);
  }
  const error = forgotSchema.safeParse({ email: 'invalid' }); expect(error.success).toBe(false);
  if (!error.success) expect(error.error.issues[0].message).toBe(t('common:m_632f0add5824'));
  expect(dateLabel('2026-10-25', true, true)).toBe(new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(2026, 9, 25, 12)));
});
test('unsupported and regional device locales resolve deterministically', () => {
  expect(resolveLanguage('xx-XX')).toBe('en'); expect(resolveLanguage('pt-PT')).toBe('pt-BR');
  expect(resolveLanguage('zh-TW')).toBe('en'); expect(resolveLanguage('de-AT')).toBe('de');
});
test('manual preference persists separately and wins account changes and stale responses', async () => {
  const write = jest.fn(async () => undefined); const changed = jest.fn();
  const p = new LanguagePreference('de-DE', { read: async () => 'ja', write }, changed);
  await p.restore(); expect(p.language).toBe('ja'); const revision = p.revision;
  await p.select('ar'); p.acceptAccount('fr', revision); p.accountChanged(); expect(p.language).toBe('ar');
  expect(write).toHaveBeenCalledWith('ar');
});
test('account beats device only until manual choice; offline selection remains immediate', async () => {
  const p = new LanguagePreference('de-DE', { read: async () => null, write: async () => { throw new Error('offline'); } }, () => undefined);
  await p.restore(); p.acceptAccount('it', p.revision); expect(p.language).toBe('it');
  await expect(p.select('ko')).rejects.toThrow('offline'); expect(p.language).toBe('ko');
  p.acceptAccount('fr', p.revision); expect(p.language).toBe('ko');
});
test('rapid storage writes are serialized and newest manual selection wins', async () => {
  const saved: string[] = []; let release!: () => void;
  const wait = new Promise<void>(resolve => { release = resolve; });
  const p = new LanguagePreference('en', { read: async () => null, write: async language => { if (language === 'fr') await wait; saved.push(language); } }, () => undefined);
  const first = p.select('fr'); const last = p.select('ja'); release(); await Promise.all([first, last]);
  expect(saved).toEqual(['fr', 'ja']); expect(p.language).toBe('ja');
});
test('language changes update visible text without remounting and preserve input drafts', async () => {
  function Form() { useLocaleRevision(); return <><AppText>{t('common:cancel')}</AppText><AppInput label="draft" defaultValue="private draft" /></>; }
  await render(<Form />); const input = screen.getByLabelText('draft'); fireEvent.changeText(input, 'unsaved');
  await act(async () => select('en')); expect(screen.getByText('Cancel')).toBeTruthy(); expect(screen.getByLabelText('draft')).toBe(input);
});
test('ProblemDetails never exposes arbitrary server title or field messages', () => {
  const error = mapApiError({ isAxiosError: true, response: { status: 400, data: { title: 'private server detail', errors: { email: ['raw server detail'] } } }, config: {} });
  expect(error.message).not.toContain('private'); expect(JSON.stringify(error)).not.toContain('raw server');
});
test.each(['light', 'dark'] as const)('language picker has fourteen accessible choices, search and immediate selection in %s', async theme => {
  useUiStore.setState({ theme }); await render(<LanguageScreen />);
  expect(screen.getAllByRole('radio')).toHaveLength(14);
  await fireEvent.changeText(screen.getByLabelText(t('settings:languageSearch')), 'Deutsch');
  expect(screen.getAllByRole('radio')).toHaveLength(1);
  await fireEvent.press(screen.getByRole('radio', { name: 'Deutsch' }));
  expect(currentLanguage()).toBe('de'); expect(screen.getByText(resources.de.settings.languageTitle)).toBeTruthy();
  expect(screen.getByRole('radio', { name: 'Deutsch', checked: true })).toBeTruthy();
  expect(reloadAppAsync).not.toHaveBeenCalled(); await act(async () => { useUiStore.setState({ theme: 'system' }); });
});
