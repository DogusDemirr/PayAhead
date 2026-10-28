import { beforeEach, test, expect, jest } from '@jest/globals';
import { render, screen, fireEvent, act } from '@testing-library/react-native';
import { router } from 'expo-router';
import RemindersScreen, { ReminderForm } from '../src/features/notifications/RemindersScreen';
import { SettingsForm } from '../src/features/notifications/NotificationsScreen';
import { useReminders, useNotificationSettings, useNotificationMutation } from '../src/features/notifications/queries';
import { ApiError } from '../src/services/api-error';
import { plan, entitlement } from './finance-fixtures';
jest.mock('../src/features/notifications/queries', () => ({ useReminders: jest.fn(), useNotificationSettings: jest.fn(), useNotificationMutation: jest.fn(), useDevices: jest.fn() }));
jest.mock('../src/features/notifications/push-runtime', () => ({ usePushState: jest.fn(), pushAvailability: () => 'Development build gerekli.' }));
jest.mock('../src/features/payments/queries', () => ({ usePayment: () => ({ data: require('./finance-fixtures').plan, refetch: jest.fn(async () => ({})) }), useEntitlements: () => ({ data: require('./finance-fixtures').entitlement }) }));
jest.mock('../src/features/notifications/TimeField', () => ({ TimeField: ({ value, onChange, disabled, error }: { value: string; onChange(value: string): void; disabled?: boolean; error?: string }) => { const { TextInput, Text, View } = require('react-native'); return <View><TextInput accessibilityLabel="Saat" value={value} onChangeText={onChange} editable={!disabled} />{error && <Text>{error}</Text>}</View>; } }));
const rule = { id: plan.id, paymentPlanId: plan.id, daysBefore: 1, timeOfDay: '09:00', isEnabled: true };
const settings = { languageCode: 'tr' as const, defaultCurrencyCode: 'TRY', timeZoneId: 'Europe/Istanbul', notificationsEnabled: true, usedTimeZoneFallback: false };
const mutateAsync = jest.fn<ReturnType<typeof useNotificationMutation>['mutateAsync']>();
function result<T>(data?: T, error?: Error) { return { data, error, isError: !!error, isRefetching: false, refetch: jest.fn(async () => ({ data, isSuccess: true })) }; }
beforeEach(() => { mutateAsync.mockReset(); mutateAsync.mockResolvedValue(rule); jest.mocked(useNotificationMutation).mockReturnValue({ mutateAsync, isPending: false } as unknown as ReturnType<typeof useNotificationMutation>); jest.mocked(useReminders).mockReturnValue(result([rule]) as unknown as ReturnType<typeof useReminders>); jest.mocked(useNotificationSettings).mockReturnValue(result(settings) as unknown as ReturnType<typeof useNotificationSettings>); });
test('reminders show actual local time/zone and open edit', async () => {
  await render(<RemindersScreen plan={plan.id} />); expect(screen.getByText('1 gün önce · 09:00')).toBeTruthy(); expect(screen.getByText('Hesap saat dilimi: Europe/Istanbul')).toBeTruthy(); await fireEvent.press(screen.getByRole('button', { name: '1 gün 09:00 düzenle' })); expect(screen.getByText('Hatırlatmayı düzenle')).toBeTruthy();
});
test('reminder list loading/empty/error have honest states', async () => {
  jest.mocked(useReminders).mockReturnValue(result() as unknown as ReturnType<typeof useReminders>); const view = await render(<RemindersScreen plan={plan.id} />); expect(screen.getByRole('progressbar')).toBeTruthy();
  jest.mocked(useReminders).mockReturnValue(result([]) as unknown as ReturnType<typeof useReminders>); await view.rerender(<RemindersScreen plan={plan.id} />); expect(screen.getByText('Henüz ödeme hatırlatması yok')).toBeTruthy();
  jest.mocked(useReminders).mockReturnValue(result(undefined, new ApiError('Çevrimdışı.', undefined, undefined, undefined, 'network')) as unknown as ReturnType<typeof useReminders>); await view.rerender(<RemindersScreen plan={plan.id} />); expect(screen.getByText('Çevrimdışı.')).toBeTruthy(); await fireEvent.press(screen.getByRole('button', { name: 'Tekrar dene' })); expect(useReminders(plan.id).refetch).toHaveBeenCalled();
});
test('create validates lead and time then sends exact local time unchanged', async () => {
  const done = jest.fn(); await render(<ReminderForm plan={plan.id} activeCount={0} limit={1} done={done} reload={async () => {}} />);
  await fireEvent.changeText(screen.getByLabelText('Kaç gün önce · 0–365'), '366'); await fireEvent.press(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })); expect(await screen.findByText('En fazla 365 gün önce olabilir.')).toBeTruthy(); expect(mutateAsync).not.toHaveBeenCalled();
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme günü' })); await fireEvent.changeText(screen.getByLabelText('Saat'), '02:30'); await fireEvent.press(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })); expect(mutateAsync).toHaveBeenCalledWith({ kind: 'saveReminder', plan: plan.id, id: undefined, body: { daysBefore: 0, timeOfDay: '02:30', isEnabled: true } }); expect(done).toHaveBeenCalledTimes(1);
});
test('edit keeps reminder id and can disable at full quota', async () => {
  await render(<ReminderForm plan={plan.id} rule={rule} activeCount={1} limit={1} done={() => {}} reload={async () => {}} />); await fireEvent(screen.getByLabelText('Hatırlatma kuralı etkin'), 'valueChange', false); await fireEvent.press(screen.getByRole('button', { name: 'Hatırlatma değişikliklerini kaydet' })); expect(mutateAsync).toHaveBeenCalledWith(expect.objectContaining({ id: rule.id, body: expect.objectContaining({ isEnabled: false }) }));
});
test('Free limit blocks active creation but inactive draft remains supported', async () => {
  await render(<ReminderForm plan={plan.id} activeCount={entitlement.limits.activeRemindersPerPlan} limit={entitlement.limits.activeRemindersPerPlan} done={() => {}} reload={async () => {}} />); expect(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })).toBeDisabled(); await fireEvent(screen.getByLabelText('Hatırlatma kuralı etkin'), 'valueChange', false); expect(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })).not.toBeDisabled();
});
test.each([new ApiError('Zaman aşımı.', undefined, undefined, undefined, 'timeout'), new ApiError('Aynı kural var.', 409)])('uncertain/conflicting reminder mutation requires reload', async error => {
  const reload = jest.fn(async () => {}); mutateAsync.mockRejectedValueOnce(error); await render(<ReminderForm plan={plan.id} activeCount={0} done={() => {}} reload={reload} />); await fireEvent.press(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })); expect(await screen.findByText(error.message)).toBeTruthy(); expect(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })).toBeDisabled(); await fireEvent.press(screen.getByRole('button', { name: 'Güncel hatırlatmaları yükle' })); expect(reload).toHaveBeenCalled();
});
test('server limit denial never claims notification sent', async () => {
  mutateAsync.mockRejectedValueOnce(new ApiError('Limit dolu.', 403, undefined, undefined, 'http', 'entitlement_limit')); await render(<ReminderForm plan={plan.id} activeCount={0} done={() => {}} reload={async () => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Hatırlatmayı kaydet' })); expect(await screen.findByText(/Etkin hatırlatma sınırına/)).toBeTruthy(); await fireEvent.press(screen.getByRole('button', { name: 'Üyelik seçeneklerini incele' })); expect(router.push).toHaveBeenCalledWith('/premium');
});
test('reminder delete requires destructive confirmation', async () => {
  await render(<RemindersScreen plan={plan.id} />); await fireEvent.press(screen.getByRole('button', { name: '1 gün 09:00 sil' })); expect(mutateAsync).not.toHaveBeenCalled(); await fireEvent.press(screen.getByRole('button', { name: 'Sil' })); expect(mutateAsync).toHaveBeenCalledWith({ kind: 'deleteReminder', plan: plan.id, id: rule.id });
});
test('settings validate timezone, preserve language and submit actual backend settings', async () => {
  mutateAsync.mockResolvedValue(settings); await render(<SettingsForm initial={settings} reload={async () => {}} />); await fireEvent.changeText(screen.getByLabelText('Hesap saat dilimi · IANA'), 'bad/timezone'); await fireEvent.press(screen.getByRole('button', { name: 'Bildirim tercihlerini kaydet' })); expect(await screen.findByText('Geçerli IANA saat dilimi gir.')).toBeTruthy(); expect(mutateAsync).not.toHaveBeenCalled();
  await fireEvent.changeText(screen.getByLabelText('Hesap saat dilimi · IANA'), 'America/New_York'); await fireEvent(screen.getByLabelText('Hesap bildirimleri etkin'), 'valueChange', false); await fireEvent.press(screen.getByRole('button', { name: 'Bildirim tercihlerini kaydet' })); expect(mutateAsync).toHaveBeenCalledWith({ kind: 'settings', body: { ...settings, timeZoneId: 'America/New_York', notificationsEnabled: false, usedTimeZoneFallback: undefined } });
});
