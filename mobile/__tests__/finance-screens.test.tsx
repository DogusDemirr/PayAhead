import { beforeEach, test, expect, jest } from '@jest/globals';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { router } from 'expo-router';
import HomeScreen from '../src/features/HomeScreen';
import PaymentsScreen from '../src/features/PaymentsScreen';
import PaymentDetailScreen from '../src/features/payments/PaymentDetailScreen';
import { PaymentForm } from '../src/features/payments/PaymentFormScreen';
import { useDashboard, usePaymentList, useFinanceClock, usePayment, usePaymentMutation } from '../src/features/payments/queries';
import { ApiError } from '../src/services/api-error';
import { plan, projected, summary, entitlement } from './finance-fixtures';
jest.mock('../src/features/payments/queries', () => ({ useDashboard: jest.fn(), usePaymentList: jest.fn(), useFinanceClock: jest.fn(), usePayment: jest.fn(), usePaymentMutation: jest.fn(), useEntitlements: jest.fn() }));
function result<T>(data?: T, error?: Error) { return { data, error, isPending: !data && !error, isError: !!error, isRefetching: false, refetch: jest.fn(async () => ({ data, isSuccess: true })) }; }
const mutateAsync = jest.fn<ReturnType<typeof usePaymentMutation>['mutateAsync']>();
beforeEach(() => {
  jest.mocked(useFinanceClock).mockReturnValue(result(summary) as unknown as ReturnType<typeof useFinanceClock>);
  jest.mocked(usePayment).mockReturnValue(result(plan) as unknown as ReturnType<typeof usePayment>);
  jest.mocked(usePaymentList).mockReturnValue(result({ items: [plan], pageNumber: 1, pageSize: 20, totalCount: 1, totalPages: 1 }) as unknown as ReturnType<typeof usePaymentList>);
  jest.mocked(usePaymentMutation).mockReturnValue({ mutateAsync, isPending: false } as unknown as ReturnType<typeof usePaymentMutation>);
  mutateAsync.mockReset(); mutateAsync.mockResolvedValue(plan);
  setDashboard(summary);
});
function setDashboard(data?: typeof summary, error?: Error) {
  jest.mocked(useDashboard).mockReturnValue({ clock: result(data) as unknown as ReturnType<typeof useDashboard>['clock'], summary: result(data, error) as unknown as ReturnType<typeof useDashboard>['summary'], upcoming: result({ context: summary.context, items: [projected], totalCount: 1 }) as unknown as ReturnType<typeof useDashboard>['upcoming'], overdue: result({ context: summary.context, items: [], totalCount: 0 }) as unknown as ReturnType<typeof useDashboard>['overdue'], hasOverdueRange: true, refresh: jest.fn(async () => {}) });
}
test('dashboard loading exposes a skeleton and never a sample total', async () => {
  setDashboard(); await render(<HomeScreen />); expect(screen.getByRole('progressbar')).toBeTruthy(); expect(screen.queryByText('Studio Music')).toBeNull();
});
test('new-user dashboard offers a real plan creation route', async () => {
  setDashboard({ ...summary, activePaymentPlanCount: 0, paidPaymentCount: 0, pendingPaymentCount: 0 });
  await render(<HomeScreen />); expect(screen.getByText('Daha sakin bir başlangıç')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme ekle' })); expect(router.push).toHaveBeenCalledWith('/add-payment');
});
test('dashboard error has retry', async () => {
  setDashboard(undefined, new ApiError('Sunucu yok.', 503));
  await render(<HomeScreen />); await fireEvent.press(screen.getByRole('button', { name: 'Tekrar dene' })); expect(useDashboard().refresh).toHaveBeenCalledTimes(1);
});
test('dashboard displays separate currencies and overdue scope from backend', async () => {
  await render(<HomeScreen />);
  expect(screen.getByText('USD')).toBeTruthy(); expect(screen.getAllByText('TRY').length).toBeGreaterThan(0); expect(screen.getByText('Bu aydan gecikenler')).toBeTruthy();
  expect(screen.getByText('BU AY · YAKLAŞAN ÖDEMELER')).toBeTruthy(); expect(screen.queryByText('Studio Music')).toBeNull();
});
test('payment list shows server count and plan status without pretending page is entire dataset', async () => {
  jest.mocked(usePaymentList).mockReturnValue(result({ items: [plan], pageNumber: 1, pageSize: 20, totalCount: 45, totalPages: 3 }) as unknown as ReturnType<typeof usePaymentList>);
  await render(<PaymentsScreen />); expect(screen.getByText('45 plan · Sayfa 1/3')).toBeTruthy(); expect(screen.getByText('Aktif plan')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'Sonraki' })); expect(usePaymentList).toHaveBeenLastCalledWith(expect.objectContaining({ pageNumber: 2 }));
});
async function fillCreateForm() {
  await fireEvent.changeText(screen.getByLabelText('Ödeme adı'), 'Ev interneti'); await fireEvent.changeText(screen.getByLabelText('Tutar'), '899,9001'); await fireEvent.changeText(screen.getByLabelText('Kategori'), 'Ev');
}
test('create form validates required fields before a mutation', async () => {
  await render(<PaymentForm today="2026-10-23" activeCount={0} reload={async () => {}} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' })); expect(await screen.findByText('Ödeme adı yaz.')).toBeTruthy(); expect(mutateAsync).not.toHaveBeenCalled();
});
test('Android-compatible decimal input submits exact amount and backend recurrence', async () => {
  await render(<PaymentForm today="2026-10-23" activeCount={0} reload={async () => {}} />); await fillCreateForm();
  expect(screen.getByLabelText('Tutar').props.keyboardType).toBe('decimal-pad');
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' }));
  expect(mutateAsync).toHaveBeenCalledWith({ kind: 'create', body: expect.objectContaining({ amount: '899.9001', frequency: 'Monthly', startDate: '2026-10-23', currencyCode: 'TRY' }) });
  expect(router.replace).toHaveBeenCalledWith({ pathname: '/payment', params: { id: plan.id } });
});
test('switching frequency hides the unused anchor and submits the matching enum', async () => {
  await render(<PaymentForm today="2026-10-23" activeCount={0} reload={async () => {}} />); await fillCreateForm();
  await fireEvent.changeText(screen.getByLabelText('Tekrar günü · isteğe bağlı'), '31');
  await fireEvent.press(screen.getByRole('button', { name: 'Haftalık' })); expect(screen.queryByLabelText('Tekrar günü · isteğe bağlı')).toBeNull();
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' })); expect(mutateAsync).toHaveBeenCalledWith(expect.objectContaining({ body: expect.objectContaining({ frequency: 'Weekly', anchorDay: null }) }));
});
test('edit keeps its original rowversion and does not overwrite on conflict', async () => {
  mutateAsync.mockRejectedValueOnce(new ApiError('Kaynak değişti.', 409)); const reload = jest.fn(async () => {});
  await render(<PaymentForm plan={plan} today="2026-10-23" activeCount={2} reload={reload} />);
  await fireEvent.changeText(screen.getByLabelText('Ödeme adı'), 'Düzenlenmiş plan');
  await fireEvent.press(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }));
  expect(mutateAsync).toHaveBeenCalledWith(expect.objectContaining({ kind: 'update', rowVersion: plan.rowVersion, body: expect.objectContaining({ name: 'Düzenlenmiş plan' }) }));
  expect(await screen.findByText('Kaynak değişti.')).toBeTruthy(); expect(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }).props.accessibilityState.disabled).toBe(true);
  await fireEvent.press(screen.getByRole('button', { name: 'Güncel kaydı yükle' })); expect(reload).toHaveBeenCalledTimes(1);
});
test('successful edit unwinds to its existing detail instead of duplicating the back stack', async () => {
  await render(<PaymentForm plan={plan} today="2026-10-23" activeCount={2} reload={async () => {}} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }));
  expect(router.dismissTo).toHaveBeenCalledWith({ pathname: '/payment', params: { id: plan.id } });
  expect(router.replace).not.toHaveBeenCalled();
});
test('free limit comes from entitlement; inactive creation remains available', async () => {
  await render(<PaymentForm today="2026-10-23" activeCount={5} entitlement={entitlement} reload={async () => {}} />);
  expect(screen.getByText('Planlarına biraz daha alan')).toBeTruthy(); expect(screen.getByRole('button', { name: 'Ödeme planını oluştur' }).props.accessibilityState.disabled).toBe(true);
  await fireEvent(screen.getByLabelText('Aktif plan'), 'valueChange', false); expect(screen.getByRole('button', { name: 'Ödeme planını oluştur' }).props.accessibilityState.disabled).toBe(false);
});
test('server entitlement error never turns into fake creation success', async () => {
  mutateAsync.mockRejectedValueOnce(new ApiError('Limit dolu.', 403, undefined, undefined, 'http', 'entitlement_limit'));
  await render(<PaymentForm today="2026-10-23" activeCount={0} reload={async () => {}} />); await fillCreateForm();
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' })); expect(await screen.findByText('Planlarına biraz daha alan')).toBeTruthy(); expect(router.replace).not.toHaveBeenCalled();
});
test('uncertain write outcome blocks blind double creation', async () => {
  mutateAsync.mockRejectedValueOnce(new ApiError('Zaman aşımı.', undefined, undefined, undefined, 'timeout'));
  await render(<PaymentForm today="2026-10-23" activeCount={0} reload={async () => {}} />); await fillCreateForm();
  await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' })); expect(await screen.findByText(/Sonuç doğrulanamadı/)).toBeTruthy();
  expect(screen.getByRole('button', { name: 'Ödeme planını oluştur' }).props.accessibilityState.disabled).toBe(true);
});
test('delete requires explicit destructive confirmation and uses current version', async () => {
  const alert = jest.spyOn(Alert, 'alert').mockImplementation(() => {});
  await render(<PaymentDetailScreen id={plan.id} />); await fireEvent.press(screen.getByRole('button', { name: 'Planı sil' })); expect(mutateAsync).not.toHaveBeenCalled();
  const buttons = alert.mock.calls[0][2]!; expect(buttons[0].style).toBe('cancel');
  await act(async () => { buttons.find((button) => button.text === 'Sil')?.onPress?.(); });
  await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ kind: 'delete', id: plan.id, rowVersion: plan.rowVersion })); alert.mockRestore();
});
