import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { router } from 'expo-router';
import AccountScreen, { PasswordForm, ProfileForm } from '../src/features/account/AccountScreen';
import { useProfile, useAccountMutation, type AccountWrite } from '../src/features/account/queries';
import { useEntitlements } from '../src/features/payments/queries';
import { ApiError } from '../src/services/api-error';
import { i18next, t, useLanguageStore } from '../src/i18n/runtime';
import { languages } from '../src/i18n/languages';
import { useUiStore } from '../src/store/ui';
import { session } from '../src/services/auth-runtime';
jest.mock('../src/features/privacy/queries', () => ({ useSecuritySummary: () => ({ data: { remainingChanges: 3, activeSessions: 1, nextAvailableAtUtc: null }, refetch: jest.fn(), isError: false }) }));
jest.mock('../src/features/account/avatar', () => ({ AvatarControl: () => null }));
jest.mock('../src/features/account/queries', () => ({ useProfile: jest.fn(), useAccountMutation: jest.fn() }));
jest.mock('../src/features/payments/queries', () => ({ useEntitlements: jest.fn() }));
jest.mock('../src/features/notifications/queries', () => ({ useNotificationSettings: () => ({ data: { timeZoneId: 'UTC' }, refetch: jest.fn() }) }));
jest.mock('../src/services/auth-runtime', () => ({ session: { invalidate: jest.fn(async () => undefined) } }));
const profile = { hasAvatar: false, id: 'aaaaaaaa-aaaa-4aaa-baaa-aaaaaaaaaaaa', displayName: 'Ada Lovelace', email: 'ada@example.invalid', rowVersion: 'AAAAAAAAAAE=' };
const mutate = jest.fn<(value: AccountWrite) => Promise<void>>(); const reset = jest.fn();
const result = (data: unknown, isError = false) => ({ data, isError, refetch: jest.fn(async () => ({})), isFetching: false, error: isError ? new ApiError('offline', undefined, undefined, undefined, 'network') : null });
beforeEach(() => {
  mutate.mockReset(); mutate.mockResolvedValue(undefined); reset.mockClear();
  jest.mocked(session.invalidate).mockClear();
  jest.mocked(useAccountMutation).mockReturnValue({ mutateAsync: mutate, isPending: false, reset } as unknown as ReturnType<typeof useAccountMutation>);
  jest.mocked(useProfile).mockReturnValue(result(profile) as unknown as ReturnType<typeof useProfile>);
  jest.mocked(useEntitlements).mockReturnValue(result({ isPremium: false, limits: { activePaymentPlans: 5, activeRemindersPerPlan: 1, activeTrackedSubscriptions: 3 } }) as unknown as ReturnType<typeof useEntitlements>);
});
afterEach(() => { void i18next.changeLanguage('tr'); useLanguageStore.setState({ language: 'tr' }); useUiStore.setState({ theme: 'system' }); jest.restoreAllMocks(); });
test.each(languages)('$code shows real profile, unchanged personal values and account sections', async ({ code }) => {
  await act(async () => { await i18next.changeLanguage(code); useLanguageStore.setState({ language: code }); });
  await render(<AccountScreen />); expect(screen.getByText('Ada Lovelace')).toBeTruthy(); expect(screen.getByText('ada@example.invalid')).toBeTruthy();
  expect(screen.getByRole('header', { name: t('settings:profile.accountTitle') })).toBeTruthy();
  expect(screen.getByLabelText(t('settings:profile.displayName')).props.value).toBe(profile.displayName);
});
test('profile validates blanks, submits only name/version and reports offline uncertainty without retry', async () => {
  const reload = jest.fn(); await render(<ProfileForm profile={profile} reload={reload} />);
  await fireEvent.changeText(screen.getByLabelText(t('settings:profile.displayName')), ' ');
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.save') })); expect(mutate).not.toHaveBeenCalled();
  await fireEvent.changeText(screen.getByLabelText(t('settings:profile.displayName')), '  Changed Name ');
  mutate.mockRejectedValueOnce(new ApiError('offline', undefined, undefined, undefined, 'network'));
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.save') }));
  expect(mutate).toHaveBeenCalledWith({ kind: 'profile', displayName: 'Changed Name', rowVersion: profile.rowVersion });
  expect(await screen.findByText(t('settings:profile.uncertainWrite'))).toBeTruthy(); expect(screen.getByRole('button', { name: t('settings:profile.save') })).toBeDisabled();
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.reload') })); expect(reload).toHaveBeenCalled();
});
test('wrong current password stays a safe localized error and clears all secret fields', async () => {
  mutate.mockRejectedValueOnce(new ApiError('raw server message', 400, undefined, undefined, 'http', 'current_password_invalid'));
  await render(<PasswordForm />);
  for (const [field, value] of [['currentPassword', 'old-password!'], ['newPassword', 'new-password!'], ['repeatPassword', 'new-password!']]) await fireEvent.changeText(screen.getByLabelText(t('settings:profile.' + field)), value);
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.changePassword') }));
  expect(await screen.findByText(t('settings:profile.wrongPassword'))).toBeTruthy();
  for (const field of ['currentPassword', 'newPassword', 'repeatPassword']) expect(screen.getByLabelText(t('settings:profile.' + field)).props.value).toBe('');
  expect(reset).toHaveBeenCalled(); expect(screen.queryByText('raw server message')).toBeNull();
});
test('delete requires password and a separate modern destructive confirmation', async () => {
  await render(<PasswordForm deleting />);
  await fireEvent.changeText(screen.getByLabelText(t('settings:profile.currentPassword')), 'correct-password!');
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.confirmDelete') })); expect(mutate).not.toHaveBeenCalled();
  await fireEvent.press(screen.getByRole('button', { name: t('common:cancel') })); expect(mutate).not.toHaveBeenCalled();
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.confirmDelete') }));
  await fireEvent.press(screen.getAllByRole('button', { name: t('settings:profile.confirmDelete') }).at(-1)!);
  await waitFor(() => expect(mutate).toHaveBeenCalledWith({ kind: 'delete', currentPassword: 'correct-password!' }));
});
test.each(['light', 'dark'] as const)('account links reuse existing screens and Android back works in %s', async theme => {
  useUiStore.setState({ theme }); await render(<AccountScreen />);
  await fireEvent.press(screen.getByRole('button', { name: t('settings:languageTitle') })); expect(router.push).toHaveBeenCalledWith('/language');
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.notifications') })); expect(router.push).toHaveBeenCalledWith('/notifications');
  await fireEvent.press(screen.getByRole('button', { name: t('common:m_b7e193af9550') })); expect(router.back).toHaveBeenCalled();
});
test('unknown entitlement is never advertised as Free or Premium', async () => {
  jest.mocked(useEntitlements).mockReturnValue(result(undefined, true) as unknown as ReturnType<typeof useEntitlements>);
  await render(<AccountScreen />); expect(screen.getByText(t('settings:profile.unknownMembership'))).toBeTruthy(); expect(screen.queryByText(t('settings:profile.free'))).toBeNull(); expect(screen.queryByText(t('settings:profile.premium'))).toBeNull();
});
test('verified premium expiry and server limits are shown', async () => {
  jest.mocked(useEntitlements).mockReturnValue(result({ isPremium: true, accessEndsAtUtc: '2099-01-01T00:00:00Z', limits: { activePaymentPlans: 1000, activeRemindersPerPlan: 10, activeTrackedSubscriptions: 1000 } }) as unknown as ReturnType<typeof useEntitlements>);
  await render(<AccountScreen />); expect(screen.getAllByText(t('settings:profile.premium'))).toHaveLength(2); expect(screen.getByText(t('settings:profile.limits', { value0: 1000, value1: 10, value2: 1000 }))).toBeTruthy();
});
test('profile success is explicit and language switching preserves unsaved profile fields', async () => {
  await render(<AccountScreen />); await fireEvent.changeText(screen.getByLabelText(t('settings:profile.displayName')), 'Unsaved nickname');
  await act(async () => { await i18next.changeLanguage('en'); useLanguageStore.setState({ language: 'en' }); });
  expect(screen.getByRole('header', { name: 'My account' })).toBeTruthy(); expect(screen.getByLabelText('Full name').props.value).toBe('Unsaved nickname');
  await fireEvent.press(screen.getByRole('button', { name: 'Save profile' })); expect(await screen.findAllByText('Profile saved.')).not.toHaveLength(0);
});
test('mismatched new passwords do not reach the API', async () => {
  await render(<PasswordForm />);
  for (const [field, value] of [['currentPassword', 'old-password!'], ['newPassword', 'new-password!'], ['repeatPassword', 'different-password!']]) await fireEvent.changeText(screen.getByLabelText(t('settings:profile.' + field)), value);
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.changePassword') })); expect(screen.getByText(t('settings:profile.passwordMismatch'))).toBeTruthy(); expect(mutate).not.toHaveBeenCalled();
});

test('exhausted normal allowance disables submission without changing account deletion', async () => {
  await render(<PasswordForm policy={{ remainingChanges: 0 }} />);
  expect(screen.getByRole('button', { name: t('settings:profile.changePassword') })).toBeDisabled();
  await render(<PasswordForm deleting policy={{ remainingChanges: 0 }} />);
  expect(screen.getByRole('button', { name: t('settings:profile.confirmDelete') })).toBeEnabled();
});

test.each(['password_reused', 'password_common', 'password_change_limit'])('%s clears secrets, refreshes quota and leaves session intact', async code => {
  mutate.mockRejectedValueOnce(new ApiError('untrusted detail', code === 'password_change_limit' ? 409 : 400, undefined, undefined, 'http', code)); const refreshPolicy = jest.fn();
  await render(<PasswordForm refreshPolicy={refreshPolicy} />);
  for (const [field, value] of [['currentPassword', 'old-password!'], ['newPassword', 'new-password!'], ['repeatPassword', 'new-password!']]) await fireEvent.changeText(screen.getByLabelText(t('settings:profile.' + field)), value);
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.changePassword') }));
  expect(refreshPolicy).toHaveBeenCalled(); expect(session.invalidate).not.toHaveBeenCalled(); expect(screen.queryByText('untrusted detail')).toBeNull();
  for (const field of ['currentPassword', 'newPassword', 'repeatPassword']) expect(screen.getByLabelText(t('settings:profile.' + field)).props.value).toBe('');
});
test('uncertain password write clears secrets and closes the local session without claiming success', async () => {
  mutate.mockRejectedValueOnce(new ApiError('offline', undefined, undefined, undefined, 'network')); await render(<PasswordForm />);
  for (const [field, value] of [['currentPassword', 'old-password!'], ['newPassword', 'new-password!'], ['repeatPassword', 'new-password!']]) await fireEvent.changeText(screen.getByLabelText(t('settings:profile.' + field)), value);
  await fireEvent.press(screen.getByRole('button', { name: t('settings:profile.changePassword') })); await waitFor(() => expect(session.invalidate).toHaveBeenCalledWith(t('settings:profile.uncertainWrite')));
  for (const field of ['currentPassword', 'newPassword', 'repeatPassword']) expect(screen.getByLabelText(t('settings:profile.' + field)).props.value).toBe('');
  expect(screen.queryByText(t('settings:profile.passwordChanged'))).toBeNull();
});
