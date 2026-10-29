import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { router, useLocalSearchParams } from 'expo-router';
import AuthScreen from '../src/features/AuthScreen';
import LegalScreen from '../src/features/privacy/LegalScreen';
import { RegistrationDocuments } from '../src/features/privacy/RegistrationDocuments';
import { PasswordStrength } from '../src/features/privacy/PasswordStrength';
import { documentIds, registrationLegalPayload, useRegistrationLegal } from '../src/features/privacy/registration-legal';
import { legalDocuments, legalVersion } from '../src/features/privacy/documents';
import { authApi } from '../src/services/auth-runtime';
import { i18next, t, useLanguageStore } from '../src/i18n/runtime';
import { languages } from '../src/i18n/languages';
import { useUiStore } from '../src/store/ui';
jest.mock('../src/services/auth-runtime', () => ({ session: { login: jest.fn() }, authApi: { register: jest.fn() } }));
jest.mock('expo-router', () => ({ router: { push: jest.fn(), replace: jest.fn(), back: jest.fn(), canGoBack: () => true }, useLocalSearchParams: jest.fn() }));
beforeEach(() => { jest.mocked(useLocalSearchParams).mockReturnValue({}); jest.mocked(authApi.register).mockReset(); });
afterEach(async () => { await act(async () => { useRegistrationLegal.setState({ attempt: null, selections: {} }); await i18next.changeLanguage('tr'); useLanguageStore.setState({ language: 'tr' }); useUiStore.setState({ theme: 'system' }); }); jest.clearAllMocks(); });
test.each(languages)('$code independently confirms three documents and returns checked statuses to registration', async ({ code }) => {
  await act(async () => { await i18next.changeLanguage(code); useLanguageStore.setState({ language: code }); });
  const attempt = useRegistrationLegal.getState().begin(); const view = await render(<RegistrationDocuments showErrors={false} />);
  for (const id of documentIds) {
    await fireEvent.press(screen.getByRole('button', { name: `${t(legalDocuments[id].title)}. ${t('settings:registrationLegal.review')}` }));
    expect(router.push).toHaveBeenLastCalledWith({ pathname: '/legal', params: { id, registration: 'true', attempt } });
    jest.mocked(useLocalSearchParams).mockReturnValue({ id, registration: 'true', attempt });
    await view.rerender(<><RegistrationDocuments showErrors={false} /><LegalScreen /></>);
    expect(screen.getByRole('checkbox', { checked: false })).toBeTruthy(); expect(screen.getByText(t(legalDocuments[id].body))).toBeTruthy();
    expect(registrationLegalPayload()).toBeNull();
    await fireEvent.press(screen.getByRole('checkbox'));
    expect(screen.getByRole('checkbox', { checked: true })).toBeTruthy();
    await fireEvent.press(screen.getByRole('button', { name: t('common:m_b7e193af9550') })); expect(router.back).toHaveBeenCalled();
    await view.rerender(<RegistrationDocuments showErrors={false} />);
    expect(screen.getByRole('button', { name: `${t(legalDocuments[id].title)}. ${t('settings:registrationLegal.confirmed')}` })).toBeTruthy();
  }
  expect(registrationLegalPayload()).toEqual({ noticeVersion: legalVersion, noticeAcknowledged: true, privacyVersion: legalVersion, privacyAcknowledged: true, termsVersion: legalVersion, termsAccepted: true });
});
test('missing confirmations keep submit clickable, prevent HTTP and highlight only missing documents', async () => {
  await render(<AuthScreen register />);
  await fireEvent.changeText(screen.getByLabelText(t('auth:m_c5176ef33f7c')), 'Test');
  await fireEvent.changeText(screen.getByLabelText(t('auth:m_e1081b69d80a')), 'test@example.invalid');
  await fireEvent.changeText(screen.getByLabelText(t('auth:m_e1ac97fff826')), 'Fictional-password-123');
  await fireEvent.changeText(screen.getByLabelText(t('settings:profile.repeatPassword')), 'Fictional-password-123');
  const state = useRegistrationLegal.getState(); await act(async () => state.toggle(state.attempt!, 'notice'));
  const button = screen.getByRole('button', { name: t('auth:m_bc5c818f2f46') }); expect(button).toBeEnabled(); await fireEvent.press(button);
  expect(authApi.register).not.toHaveBeenCalled(); expect(screen.getByText(t('settings:registrationLegal.required'))).toBeTruthy();
  expect(screen.getByRole('button', { name: `${t(legalDocuments.privacy.title)}. ${t('settings:registrationLegal.missing')}` })).toBeTruthy();
  expect(screen.getByRole('button', { name: `${t(legalDocuments.notice.title)}. ${t('settings:registrationLegal.confirmed')}` })).toBeTruthy();
});
test.each(['light', 'dark'] as const)('%s checkbox can be unchecked and stale/deep links cannot mutate a new registration', async theme => {
  useUiStore.setState({ theme }); const first = useRegistrationLegal.getState().begin();
  jest.mocked(useLocalSearchParams).mockReturnValue({ id: 'terms', registration: 'true', attempt: first }); const view = await render(<LegalScreen />);
  await fireEvent.press(screen.getByRole('checkbox')); await fireEvent.press(screen.getByRole('checkbox')); expect(screen.getByRole('checkbox', { checked: false })).toBeTruthy();
  await act(async () => { useRegistrationLegal.getState().begin(); }); expect(screen.queryByRole('checkbox')).toBeNull();
  await view.unmount(); await render(<AuthScreen register />); expect(registrationLegalPayload()).toBeNull();
});
test('strength stays hidden for empty input, appears while typing and disappears when cleared', async () => {
  const view = await render(<PasswordStrength password="" />); expect(screen.queryByRole('progressbar')).toBeNull(); expect(screen.queryByText(t('settings:security.weak'))).toBeNull();
  await view.rerender(<PasswordStrength password="a" />); expect(screen.getByRole('progressbar').props.accessibilityValue.now).toBe(1);
  await view.rerender(<PasswordStrength password="Long fictional candidate 987!" />); expect(screen.getByRole('progressbar').props.accessibilityValue.now).toBe(3);
  await view.rerender(<PasswordStrength password="" />); expect(screen.queryByRole('progressbar')).toBeNull(); expect(screen.queryByText(t('settings:security.strong'))).toBeNull();
});
