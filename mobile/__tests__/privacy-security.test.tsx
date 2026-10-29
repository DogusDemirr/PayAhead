import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import PrivacyScreen, { SecurityStatus } from '../src/features/privacy/PrivacyScreen';
import LegalScreen from '../src/features/privacy/LegalScreen';
import { PasswordStrength } from '../src/features/privacy/PasswordStrength';
import { legalDocuments, legalVersion } from '../src/features/privacy/documents';
import { securitySchema, useSecuritySummary } from '../src/features/privacy/queries';
import { i18next, t, useLanguageStore } from '../src/i18n/runtime';
import { languages } from '../src/i18n/languages';
import { useUiStore } from '../src/store/ui';
import { ApiError, mapApiError } from '../src/services/api-error';
jest.mock('../src/features/privacy/queries', () => ({ ...jest.requireActual<object>('../src/features/privacy/queries'), useSecuritySummary: jest.fn() }));
jest.mock('expo-router', () => ({ router: { push: jest.fn(), back: jest.fn(), canGoBack: () => true }, useLocalSearchParams: jest.fn() }));
const refetch = jest.fn();
const policy = { remainingChanges: 0, changeLimit: 3, windowDays: 30, nextAvailableAtUtc: '2026-11-28T12:00:00Z', asOfUtc: '2026-10-29T12:00:00Z', activeSessions: 2 };
beforeEach(() => {
  jest.mocked(useLocalSearchParams).mockReturnValue({ id: 'notice' });
  jest.mocked(useSecuritySummary).mockReturnValue({ data: policy, isError: false, refetch } as unknown as ReturnType<typeof useSecuritySummary>);
});
afterEach(async () => { await act(async () => { await i18next.changeLanguage('tr'); useLanguageStore.setState({ language: 'tr' }); }); useUiStore.setState({ theme: 'system' }); jest.clearAllMocks(); });
test.each(languages)('$code renders draft documents, real quota and RTL text without truncation', async ({ code, rtl }) => {
  await act(async () => { await i18next.changeLanguage(code); useLanguageStore.setState({ language: code }); });
  await render(<PrivacyScreen />);
  expect(screen.getByRole('header', { name: t('settings:security.title') })).toBeTruthy();
  expect(screen.getByText(t('settings:security.remaining', { value0: 0 }))).toBeTruthy();
  expect(screen.getByText(t('settings:security.sessions', { value0: 2 }))).toBeTruthy();
  for (const [id, doc] of Object.entries(legalDocuments)) {
    await fireEvent.press(screen.getByRole('button', { name: t(doc.title) }));
    expect(router.push).toHaveBeenLastCalledWith({ pathname: '/legal', params: { id } });
  }
  await fireEvent.press(screen.getByRole('button', { name: t('settings:security.recovery') })); expect(router.push).toHaveBeenLastCalledWith('/forgot-password');
  await render(<LegalScreen />);
  const text = screen.getByText(t('settings:legal.noticeBody'));
  expect(StyleSheet.flatten(text.props.style).writingDirection).toBe(rtl ? 'rtl' : 'ltr'); expect(text.props.numberOfLines).toBeUndefined();
  expect(screen.getByText(t('settings:legal.draft'))).toBeTruthy(); expect(screen.getByText(legalVersion)).toBeTruthy();
});
test.each(['light', 'dark'] as const)('%s legal screen separates terms and information with Android back', async theme => {
  useUiStore.setState({ theme }); jest.mocked(useLocalSearchParams).mockReturnValue({ id: 'terms' }); await render(<LegalScreen />);
  expect(screen.getByText(t('settings:legal.termsBody'))).toBeTruthy(); expect(screen.queryByText(t('settings:legal.retention'))).toBeNull();
  await fireEvent.press(screen.getByRole('button', { name: t('common:m_b7e193af9550') })); expect(router.back).toHaveBeenCalled();
});
test('unknown document falls back safely and offline never invents remaining allowance', async () => {
  jest.mocked(useLocalSearchParams).mockReturnValue({ id: '__proto__' }); await render(<LegalScreen />); expect(screen.getByText(t('settings:legal.noticeBody'))).toBeTruthy();
  jest.mocked(useSecuritySummary).mockReturnValue({ isError: true, error: new ApiError('offline', undefined, undefined, undefined, 'network'), refetch } as unknown as ReturnType<typeof useSecuritySummary>);
  await render(<SecurityStatus />); expect(screen.queryByText(t('settings:security.remaining', { value0: 3 }))).toBeNull();
  await fireEvent.press(screen.getByRole('button', { name: t('payments:m_0ce9380b07f8') })); expect(refetch).toHaveBeenCalled();
});
test('strength is an accessible estimate and never displays the candidate', async () => {
  const candidate = 'Long fictional candidate 987!'; await render(<PasswordStrength password={candidate} />);
  expect(screen.getByRole('progressbar').props.accessibilityValue).toMatchObject({ now: 3, text: t('settings:security.strong') }); expect(screen.queryByText(candidate)).toBeNull();
});
test.each([['password_reused', 400, 'reused'], ['password_common', 400, 'common'], ['password_change_limit', 409, 'limit']])('%s is mapped to safe translated text', (code, status, key) => {
  const mapped = mapApiError({ isAxiosError: true, response: { status, data: { code, detail: 'untrusted server detail' } }, config: { url: '/api/users/me/change-password' } });
  expect(mapped.message).toBe(t(`settings:security.${key}`)); expect(mapped.message).not.toContain('untrusted');
});
test('security response rejects fabricated quota values and invalid dates before rendering', () => {
  expect(securitySchema.safeParse(policy).success).toBe(true); expect(securitySchema.safeParse({ ...policy, remainingChanges: 4 }).success).toBe(false);
  expect(securitySchema.safeParse({ ...policy, nextAvailableAtUtc: 'invalid' }).success).toBe(false);
});
test('unexpected HTTP status cannot be disguised as an exhausted password quota', () => {
  const mapped = mapApiError({ isAxiosError: true, response: { status: 500, data: { code: 'password_change_limit' } }, config: { url: '/api/users/me/change-password' } });
  expect(mapped.message).not.toBe(t('settings:security.limit')); expect(mapped).toMatchObject({ status: 500 });
});
