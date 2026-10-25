import { beforeEach, test, expect, jest } from '@jest/globals';
import { render, screen, fireEvent, act, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { router } from 'expo-router';
import SubscriptionsScreen from '../src/features/SubscriptionsScreen';
import TrialsScreen from '../src/features/subscriptions/TrialsScreen';
import { SubscriptionForm } from '../src/features/subscriptions/SubscriptionFormScreen';
import { SubscriptionDetail } from '../src/features/subscriptions/SubscriptionDetailScreen';
import { SubscriptionDashboardCard, SubscriptionTotals, accessHint, trialHint } from '../src/features/subscriptions/components';
import { useSubscriptions, useSubscriptionSummary, useExpiringTrials, useSubscriptionMutation } from '../src/features/subscriptions/queries';
import { ApiError } from '../src/services/api-error';
import { subscription, subscriptionSummary } from './subscription-fixtures';
import { entitlement } from './finance-fixtures';
jest.mock('../src/features/catalog/CatalogPicker', () => ({ CatalogSelectionField: ({ onSelect }: { onSelect(value: unknown): void }) => { const { Pressable, Text } = require('react-native'); return <Pressable accessibilityRole="button" accessibilityLabel="Test özel kategorisini seç" onPress={() => onSelect({ category: { id: 'c1111111-1111-4111-8111-111111111111', nameTr: 'Diğer' }, service: null })}><Text>Kategori seç</Text></Pressable>; } }));
jest.mock('../src/features/subscriptions/queries', () => ({ useSubscriptions: jest.fn(), useSubscriptionSummary: jest.fn(), useExpiringTrials: jest.fn(), useSubscriptionMutation: jest.fn() }));
jest.mock('../src/features/payments/queries', () => ({ useEntitlements: () => ({ data: undefined, refetch: jest.fn() }) }));
jest.mock('../src/components/date-field', () => ({ DateField: ({ label, value, onChange, disabled, error }: { label: string; value: string; onChange: (value: string) => void; disabled?: boolean; error?: string }) => { const { TextInput, Text, View } = require('react-native'); return <View><TextInput accessibilityLabel={label} value={value} onChangeText={onChange} editable={!disabled} />{error && <Text>{error}</Text>}</View>; } }));
function result<T>(data?: T, error?: Error) { return { data, error, isError: !!error, isRefetching: false, refetch: jest.fn(async () => ({ data, isSuccess: true })) }; }
const mutateAsync = jest.fn<ReturnType<typeof useSubscriptionMutation>['mutateAsync']>();
beforeEach(() => {
  jest.mocked(useSubscriptions).mockReturnValue(result({ items: [subscription], pageNumber: 1, pageSize: 20, totalCount: 21, totalPages: 2 }) as unknown as ReturnType<typeof useSubscriptions>);
  jest.mocked(useSubscriptionSummary).mockReturnValue(result(subscriptionSummary) as unknown as ReturnType<typeof useSubscriptionSummary>);
  jest.mocked(useExpiringTrials).mockReturnValue(result([]) as unknown as ReturnType<typeof useExpiringTrials>);
  mutateAsync.mockReset(); mutateAsync.mockResolvedValue(subscription);
  jest.mocked(useSubscriptionMutation).mockReturnValue({ mutateAsync, isPending: false } as unknown as ReturnType<typeof useSubscriptionMutation>);
});
async function fill() { await fireEvent.changeText(screen.getByLabelText('Abonelik adı'), 'Real subscription'); await fireEvent.changeText(screen.getByLabelText('Sağlayıcı adı'), 'Real provider'); await fireEvent.changeText(screen.getByLabelText('Abonelik tutarı'), '199,9001'); await fireEvent.press(screen.getByRole('button', { name: 'Test özel kategorisini seç' })); }
const formProps = { today: '2026-10-24', trackingCount: 0, paymentCount: 0, reload: async () => {} };
test('real list uses server pagination/status/currency and detail navigation', async () => {
  await render(<SubscriptionsScreen />); expect(screen.getByText('21 abonelik · Sayfa 1/2')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'Sonraki abonelik sayfası' })); expect(useSubscriptions).toHaveBeenLastCalledWith(expect.objectContaining({ pageNumber: 2 }));
  await fireEvent.press(screen.getByRole('button', { name: 'Ücretsiz deneme' })); expect(useSubscriptions).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Trial', pageNumber: 1 }));
  await fireEvent.changeText(screen.getByLabelText('Abonelik para birimi filtresi'), 'USD'); await fireEvent.press(screen.getByRole('button', { name: 'Abonelik filtresini uygula' })); expect(useSubscriptions).toHaveBeenLastCalledWith(expect.objectContaining({ currencyCode: 'USD' }));
  await fireEvent.press(screen.getAllByRole('button', { name: 'Test Music, Ücretsiz deneme, detay' })[0]); expect(router.push).toHaveBeenCalledWith({ pathname: '/subscription', params: { id: subscription.id } });
});
test('list empty/loading/error are genuine states and error supports retry', async () => {
  jest.mocked(useSubscriptions).mockReturnValue(result(undefined, new ApiError('Bağlantı yok.', 503)) as unknown as ReturnType<typeof useSubscriptions>);
  const view = await render(<SubscriptionsScreen />); expect(screen.getByText('Bağlantı yok.')).toBeTruthy(); await fireEvent.press(screen.getByRole('button', { name: 'Tekrar dene' })); expect(useSubscriptions({ pageNumber: 1, pageSize: 20 }).refetch).toHaveBeenCalled();
  jest.mocked(useSubscriptions).mockReturnValue(result({ items: [], totalCount: 0, totalPages: 0, pageSize: 20, pageNumber: 1 }) as unknown as ReturnType<typeof useSubscriptions>); await view.rerender(<SubscriptionsScreen />); expect(screen.getByText('Burada henüz abonelik yok')).toBeTruthy();
  jest.mocked(useSubscriptions).mockReturnValue(result() as unknown as ReturnType<typeof useSubscriptions>); await view.rerender(<SubscriptionsScreen />); expect(screen.getByRole('progressbar')).toBeTruthy();
});
test('summary separates currencies and labels monthly estimate explicitly', async () => {
  await render(<SubscriptionTotals data={subscriptionSummary} />); expect(screen.getByText('TRY / ay')).toBeTruthy(); expect(screen.getByText('USD / ay')).toBeTruthy(); expect(screen.getByText(/bu ayın kesin harcaması değildir/)).toBeTruthy();
});
test('dashboard subscription card uses server summary and opens upcoming trials', async () => {
  await render(<SubscriptionDashboardCard />); expect(screen.getByText('0 aktif · 1 deneme')).toBeTruthy(); expect(screen.getByText(/Sonraki yenileme: Test Music/)).toBeTruthy(); expect(screen.getByText(/finansal özete bağlı plan üzerinden zaten dahildir/)).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: '1 yaklaşan deneme bitişi' })); expect(router.push).toHaveBeenCalledWith('/subscription-trials');
});
test('trial countdown handles last day, historical trial and cancelled continuing access', () => {
  expect(trialHint({ ...subscription, trialDaysRemaining: 0 })).toBe('Denemenin son günü');
  expect(trialHint({ ...subscription, status: 'Active', trialEndDate: '2026-10-23' })).toBe('Deneme süresi bitti.');
  expect(accessHint({ ...subscription, status: 'Cancelled', accessEndsOn: '2026-10-24' })).toContain('devam ediyor');
  expect(accessHint({ ...subscription, status: 'Expired', accessEndsOn: '2026-10-23' })).toContain('sona erdi');
});
test('create validates before sending, then submits exact amount once without a payment request', async () => {
  await render(<SubscriptionForm {...formProps} />); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(await screen.findByText('Abonelik adı yaz.')).toBeTruthy(); expect(mutateAsync).not.toHaveBeenCalled();
  await fill(); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(mutateAsync).toHaveBeenCalledTimes(1); expect(mutateAsync).toHaveBeenCalledWith({ kind: 'create', body: expect.objectContaining({ amount: '199.9001', billingFrequency: 'Monthly', firstBillingDate: '2026-10-24', trialEndDate: null }) }); expect(router.replace).toHaveBeenCalledWith({ pathname: '/subscription', params: { id: subscription.id } });
});
test('trial form keeps first paid after trial; weekly recurrence drops anchor', async () => {
  await render(<SubscriptionForm {...formProps} />); await fill(); await fireEvent.press(screen.getByRole('button', { name: 'Ücretsiz deneme' }));
  await fireEvent.changeText(screen.getByLabelText('Deneme bitişi'), '2026-10-31'); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(await screen.findByText('İlk ücretli tarih deneme bitişinden sonra olmalı.')).toBeTruthy(); expect(mutateAsync).not.toHaveBeenCalled();
  await fireEvent.changeText(screen.getByLabelText('İlk ücretli ödeme tarihi'), '2026-11-01'); await fireEvent.press(screen.getByRole('button', { name: 'Haftalık' })); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(mutateAsync).toHaveBeenCalledWith(expect.objectContaining({ body: expect.objectContaining({ status: 'Trial', trialStartDate: '2026-10-24', trialEndDate: '2026-10-31', firstBillingDate: '2026-11-01', anchorDay: null }) }));
});
test('tracking and linked-plan limits independently block creation; Premium limit is server supplied', async () => {
  const view = await render(<SubscriptionForm {...formProps} trackingCount={entitlement.limits.activeTrackedSubscriptions} entitlement={entitlement} />); expect(screen.getByRole('button', { name: 'Aboneliği oluştur' })).toBeDisabled();
  await view.rerender(<SubscriptionForm {...formProps} paymentCount={entitlement.limits.activePaymentPlans} entitlement={entitlement} />); expect(screen.getByRole('button', { name: 'Aboneliği oluştur' })).toBeDisabled();
  await view.rerender(<SubscriptionForm {...formProps} trackingCount={3} entitlement={{ ...entitlement, isPremium: true, plan: 'Premium', limits: { ...entitlement.limits, activeTrackedSubscriptions: 100 } }} />); expect(screen.getByRole('button', { name: 'Aboneliği oluştur' })).not.toBeDisabled();
});
test.each([new ApiError('Kaynak değişti.', 409), new ApiError('Zaman aşımı.', undefined, undefined, undefined, 'timeout')])('uncertain edit blocks stale retry and preserves original rowVersion', async error => {
  mutateAsync.mockRejectedValueOnce(error); const reload = jest.fn(async () => {}); await render(<SubscriptionForm {...formProps} record={subscription} reload={reload} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Abonelik değişikliklerini kaydet' })); expect(await screen.findByText(error.message)).toBeTruthy(); expect(mutateAsync).toHaveBeenCalledWith(expect.objectContaining({ kind: 'update', rowVersion: subscription.rowVersion })); expect(screen.getByRole('button', { name: 'Abonelik değişikliklerini kaydet' })).toBeDisabled(); await fireEvent.press(screen.getByRole('button', { name: 'Güncel kaydı yükle' })); expect(reload).toHaveBeenCalledTimes(1);
});
test('server entitlement denial remains an error and never navigates to fake success', async () => {
  mutateAsync.mockRejectedValueOnce(new ApiError('Limit dolu.', 403, undefined, undefined, 'http', 'entitlement_limit')); await render(<SubscriptionForm {...formProps} />); await fill(); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(await screen.findByText('Aktif kayıt sınırına ulaştın')).toBeTruthy(); expect(router.replace).not.toHaveBeenCalled();
});
test('successful edit unwinds to existing detail and retains editable backend fields', async () => {
  await render(<SubscriptionForm {...formProps} record={subscription} />); await fireEvent.changeText(screen.getByLabelText('Sağlayıcı adı'), 'Updated provider'); await fireEvent.press(screen.getByRole('button', { name: 'Abonelik değişikliklerini kaydet' }));
  expect(mutateAsync).toHaveBeenCalledWith(expect.objectContaining({ kind: 'update', rowVersion: subscription.rowVersion, body: expect.objectContaining({ providerName: 'Updated provider', trialEndDate: '2026-10-31', firstBillingDate: '2026-11-01' }) })); expect(router.dismissTo).toHaveBeenCalledWith({ pathname: '/subscription', params: { id: subscription.id } });
});
test('cancel requires explicit access date and external-provider warning; double confirmation sends once', async () => {
  const alert = jest.spyOn(Alert, 'alert').mockImplementation(() => {}); let finish!: (value: typeof subscription) => void; mutateAsync.mockImplementationOnce(() => new Promise(resolve => { finish = resolve; }));
  await render(<SubscriptionDetail initial={subscription} reload={async () => {}} />); expect(screen.getByRole('button', { name: 'Takip kaydını iptal et' })).toBeDisabled();
  await fireEvent.changeText(screen.getByLabelText('İptal sonrası erişimin son günü'), '2026-11-10'); await fireEvent.press(screen.getByRole('button', { name: 'Takip kaydını iptal et' })); expect(mutateAsync).not.toHaveBeenCalled(); expect(alert.mock.calls[0][1]).toContain('Dış sağlayıcıda iptal yapılmaz');
  const confirm = alert.mock.calls[0][2]!.find(x => x.text === 'Takibi iptal et')!.onPress!; await act(async () => { confirm(); confirm(); }); expect(mutateAsync).toHaveBeenCalledTimes(1); expect(mutateAsync).toHaveBeenCalledWith({ kind: 'cancel', id: subscription.id, rowVersion: subscription.rowVersion, accessEndsOn: '2026-11-10' });
  await act(async () => finish({ ...subscription, status: 'Cancelled', autoRenew: false, nextBillingDate: null, accessEndsOn: '2026-11-10' })); expect(await screen.findByText(/dahil erişim devam ediyor/)).toBeTruthy(); expect(screen.queryByRole('button', { name: 'Aboneliği düzenle' })).toBeNull(); alert.mockRestore();
});
test('delete uses strong version only after confirmation and returns to list', async () => {
  const alert = jest.spyOn(Alert, 'alert').mockImplementation(() => {}); mutateAsync.mockResolvedValueOnce(undefined); await render(<SubscriptionDetail initial={subscription} reload={async () => {}} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Takip kaydını kaldır' })); expect(alert.mock.calls[0][1]).toContain('Ödeme geçmişi korunur'); await act(async () => alert.mock.calls[0][2]!.find(x => x.text === 'Kaldır')!.onPress!()); await waitFor(() => expect(router.dismissTo).toHaveBeenCalledWith('/(tabs)/subscriptions')); expect(mutateAsync).toHaveBeenCalledWith({ kind: 'delete', id: subscription.id, rowVersion: subscription.rowVersion }); alert.mockRestore();
});
test('expired detail does not offer edit/cancel and linked plan opens read-only', async () => {
  await render(<SubscriptionDetail initial={{ ...subscription, status: 'Expired', accessEndsOn: '2026-10-23' }} reload={async () => {}} />); expect(screen.queryByRole('button', { name: 'Aboneliği düzenle' })).toBeNull(); expect(screen.queryByRole('button', { name: 'Takip kaydını iptal et' })).toBeNull(); await fireEvent.press(screen.getByRole('button', { name: 'Bağlı ödeme planını gör' })); expect(router.push).toHaveBeenCalledWith({ pathname: '/payment', params: { id: subscription.paymentPlanId, readOnly: 'true' } });
});
test('cancel conflict offers reload and prevents stale cancellation retry', async () => {
  const alert = jest.spyOn(Alert, 'alert').mockImplementation(() => {}); const reload = jest.fn(async () => {}); mutateAsync.mockRejectedValueOnce(new ApiError('Kaynak değişti.', 409));
  await render(<SubscriptionDetail initial={{ ...subscription, accessEndsOn: '2026-11-10' }} reload={reload} />); await fireEvent.press(screen.getByRole('button', { name: 'Takip kaydını iptal et' })); await act(async () => alert.mock.calls[0][2]!.find(x => x.text === 'Takibi iptal et')!.onPress!()); expect(await screen.findByText('Kaynak değişti.')).toBeTruthy(); expect(screen.getByRole('button', { name: 'Takip kaydını iptal et' })).toBeDisabled(); await fireEvent.press(screen.getByRole('button', { name: 'Güncel kaydı yükle' })); expect(reload).toHaveBeenCalledTimes(1); alert.mockRestore();
});
test('expiring trials use server 1/3/7 windows with no fake notification claim', async () => {
  jest.mocked(useExpiringTrials).mockReturnValue(result([{ subscriptionId: subscription.id, name: subscription.name, providerName: subscription.providerName, trialEndDate: '2026-10-24', daysRemaining: 0, firstBillingDate: '2026-10-25', amount: subscription.amount, currencyCode: 'TRY' }]) as unknown as ReturnType<typeof useExpiringTrials>);
  await render(<TrialsScreen />); expect(screen.getByText('Denemenin son günü')).toBeTruthy(); expect(screen.getByText(/İlk ücretli ödeme:/)).toBeTruthy(); for (const days of [1, 3, 7]) { await fireEvent.press(screen.getByRole('button', { name: new RegExp(`^${days} gün içinde`) })); expect(useExpiringTrials).toHaveBeenLastCalledWith(days); }
});
