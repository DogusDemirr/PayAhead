import { render, screen, fireEvent, waitFor, act } from '@testing-library/react-native';
import { beforeEach, test, expect, jest } from '@jest/globals';
import { useSessionStore } from '../src/store/session';
import { session, authApi } from '../src/services/auth-runtime';
import { ApiError } from '../src/services/api-error';
import { router } from 'expo-router';
jest.mock('../src/services/auth-runtime', () => ({ session: { login: jest.fn(), logout: jest.fn() }, authApi: { register: jest.fn() } }));
jest.mock('../src/hooks/use-current-user', () => ({ useCurrentUser: () => ({ data: { id: 'test-id', displayName: 'Test User', email: 'test@example.com' }, isError: false, refetch: jest.fn() }) }));
import { useUiStore } from '../src/store/ui';
import { documentIds, useRegistrationLegal } from '../src/features/privacy/registration-legal';

import AuthScreen from '../src/features/AuthScreen';

import SettingsScreen from '../src/features/SettingsScreen';
import SubscriptionsScreen from '../src/features/SubscriptionsScreen';
jest.mock('../src/features/subscriptions/queries', () => ({ useSubscriptions: () => ({ data: { items: [], totalCount: 0, totalPages: 0 }, refetch: jest.fn() }), useSubscriptionSummary: () => ({ data: undefined, refetch: jest.fn() }) }));
jest.mock('../src/features/payments/queries', () => ({ useEntitlements: () => ({ data: undefined, refetch: jest.fn() }) }));

import { canUseDesignPreview } from '../src/services/preview-policy';
import { currencyTotals, dateLabel, money } from '../src/utils/format';

beforeEach(() => { useUiStore.setState({ theme: 'light', preview: 'demo' }); useSessionStore.setState({ status: 'authenticated', user: null, notice: null }); jest.mocked(session.login).mockResolvedValue(undefined); });

test('preview requires development and an explicit flag', () => {
  expect(canUseDesignPreview(false, 'true')).toBe(false);
  expect(canUseDesignPreview(true, undefined)).toBe(false);
  expect(canUseDesignPreview(true, 'true')).toBe(true);
});
test('currencies remain separate and Turkish formatting preserves the calendar day', () => {
  expect(currencyTotals([{ amount: 10, currencyCode: 'TRY' }, { amount: 5, currencyCode: 'USD' }, { amount: 2, currencyCode: 'TRY' }])).toEqual([{ currencyCode: 'TRY', amount: 12 }, { currencyCode: 'USD', amount: 5 }]);
  expect(money(1234.5)).toContain('1.234,50');
  expect(dateLabel('2026-01-01', true)).toBe('1 Ocak');
});




test('valid login form submits exact credentials to real-session boundary', async () => {
  await render(<AuthScreen />);
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'preview@example.com');
  await fireEvent.changeText(screen.getByLabelText('Şifre'), 'Example-only-12345');
  await fireEvent.press(screen.getByRole('button', { name: 'Giriş yap' }));
  expect(session.login).toHaveBeenCalledWith({ email: 'preview@example.com', password: 'Example-only-12345' });
});
test('invalid registration exposes validation errors', async () => {
  await render(<AuthScreen register />);
  await fireEvent.press(screen.getByRole('button', { name: 'Hesap oluştur' }));
  expect(await screen.findByText('Adını yaz.')).toBeTruthy();
  expect(screen.getByText('Geçerli bir e-posta adresi yaz.')).toBeTruthy();
});


test('login links to the real password reset request screen', async () => {
  await render(<AuthScreen />);
  await fireEvent.press(screen.getByRole('button', { name: 'Şifremi unuttum' }));
  expect(router.push).toHaveBeenCalledWith('/forgot-password');
});
test('theme selection updates UI preference without creating account state', async () => {
  await render(<SettingsScreen />);
  await fireEvent.press(screen.getByRole('radio', { name: 'Koyu' }));
  expect(useUiStore.getState().theme).toBe('dark');
  expect(screen.getByText('Test User')).toBeTruthy();
});
test('login rejects an empty password without calling authentication', async () => {
  await render(<AuthScreen />);
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'test@example.com');
  await fireEvent.press(screen.getByRole('button', { name: 'Giriş yap' }));
  expect(await screen.findByText('Şifreni yaz.')).toBeTruthy();
  expect(session.login).not.toHaveBeenCalled();
});
test('invalid credentials are shown without exposing transport details', async () => {
  jest.mocked(session.login).mockRejectedValueOnce(new ApiError('E-posta veya şifre hatalı.', 401));
  await render(<AuthScreen />);
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'test@example.com');
  await fireEvent.changeText(screen.getByLabelText('Şifre'), 'password');
  await fireEvent.press(screen.getByRole('button', { name: 'Giriş yap' }));
  expect(await screen.findByText('E-posta veya şifre hatalı.')).toBeTruthy();
});
test('registration returns to login and does not assume automatic login', async () => {
  jest.mocked(authApi.register).mockResolvedValueOnce({ id: 'test', displayName: 'Test', email: 'test@example.com' });
  await render(<AuthScreen register />);
  await fireEvent.changeText(screen.getByLabelText('Adın'), 'Test');
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'test@example.com');
  await fireEvent.changeText(screen.getByLabelText('Şifre'), 'Example-password-123');
  if (screen.queryByLabelText('Yeni şifre tekrar')) await fireEvent.changeText(screen.getByLabelText('Yeni şifre tekrar'), 'Example-password-123');
  await act(async () => { const state = useRegistrationLegal.getState(); for (const id of documentIds) state.toggle(state.attempt!, id); });
  await fireEvent.press(screen.getByRole('button', { name: 'Hesap oluştur' }));
  expect(authApi.register).toHaveBeenCalledWith({ email: 'test@example.com', displayName: 'Test', password: 'Example-password-123', legalVersion: '2026-10-29-draft-1', legal: { noticeVersion: '2026-10-29-draft-1', noticeAcknowledged: true, privacyVersion: '2026-10-29-draft-1', privacyAcknowledged: true, termsVersion: '2026-10-29-draft-1', termsAccepted: true } });
  expect(router.replace).toHaveBeenCalledWith({ pathname: '/(auth)/login', params: { registered: 'true' } });
  expect(session.login).not.toHaveBeenCalled();
});
test('backend register validation is attached to the matching input', async () => {
  jest.mocked(authApi.register).mockRejectedValueOnce(new ApiError('İstek doğrulanamadı.', 400, 'test-trace', { email: ['Sunucu e-posta kuralı.'] }));
  await render(<AuthScreen register />);
  await fireEvent.changeText(screen.getByLabelText('Adın'), 'Test');
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'test@example.com');
  await fireEvent.changeText(screen.getByLabelText('Şifre'), 'Example-password-123');
  if (screen.queryByLabelText('Yeni şifre tekrar')) await fireEvent.changeText(screen.getByLabelText('Yeni şifre tekrar'), 'Example-password-123');
  await act(async () => { const state = useRegistrationLegal.getState(); for (const id of documentIds) state.toggle(state.attempt!, id); });
  await fireEvent.press(screen.getByRole('button', { name: 'Hesap oluştur' }));
  await waitFor(() => expect(screen.getByText('Bu alanı kontrol edip geçerli bir değer gir.')).toBeTruthy());
});
test('overlapping UI submit events are gated before creating a session', async () => {
  let finish!: () => void;
  jest.mocked(session.login).mockReturnValueOnce(new Promise<void>((resolve) => { finish = resolve; }));
  await render(<AuthScreen />);
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'test@example.com');
  await fireEvent.changeText(screen.getByLabelText('Şifre'), 'password');
  const submit = screen.getByLabelText('Şifre').props.onSubmitEditing;
  await act(async () => { setTimeout(finish, 0); await Promise.all([submit(), submit()]); });
  expect(session.login).toHaveBeenCalledTimes(1);
});
test('subscriptions empty state excludes sample providers', async () => {
  useUiStore.setState({ preview: 'empty' });
  await render(<SubscriptionsScreen />);
  expect(screen.queryByText('Studio Music')).toBeNull();
  expect(screen.queryByText('Cloud Space')).toBeNull();
});
