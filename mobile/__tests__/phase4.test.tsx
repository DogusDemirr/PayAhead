import { beforeEach, expect, jest, test } from '@jest/globals';
import { render, screen, fireEvent, act } from '@testing-library/react-native';
import { router } from 'expo-router';
import DateTimePicker from '@react-native-community/datetimepicker';
import { CalendarBody } from '../src/features/CalendarScreen';
import { HistoryBody } from '../src/features/occurrences/HistoryScreen';
import { OccurrenceBody } from '../src/features/occurrences/OccurrenceScreen';
import { AnalyticsBody } from '../src/features/analytics/AnalyticsScreen';
import PasswordResetScreen from '../src/features/PasswordResetScreen';
import { DateField, localCalendarDate, calendarDateKey } from '../src/components/date-field';
import { barRatios } from '../src/features/analytics/Charts';
import { useCalendar, useHistory, useOccurrenceMutation, useAnalytics } from '../src/features/payments/queries';
import { passwordResetApi, extractResetToken } from '../src/services/password-reset';
import { session } from '../src/services/auth-runtime';
import { ApiError } from '../src/services/api-error';
import type { Occurrence, CurrencyAnalytics } from '../src/features/payments/contracts';
import { plan, projected, summary } from './finance-fixtures';
jest.mock('../src/features/payments/queries', () => ({ useCalendar: jest.fn(), useHistory: jest.fn(), useOccurrenceMutation: jest.fn(), useAnalytics: jest.fn() }));
jest.mock('../src/services/password-reset', () => ({ ...jest.requireActual<typeof import('../src/services/password-reset')>('../src/services/password-reset'), passwordResetApi: { forgot: jest.fn(), reset: jest.fn() } }));
jest.mock('../src/services/auth-runtime', () => ({ session: { invalidate: jest.fn(async () => {}) } }));
jest.mock('@react-native-community/datetimepicker', () => ({ __esModule: true, default: jest.fn(() => null) }));
const today = '2026-10-23';
const occurrence: Occurrence = { id: null, paymentPlanId: plan.id, dueDate: projected.dueDate, status: 'Pending', actualAmount: null, paidAtUtc: null, rowVersion: null, createdAtUtc: null, isRecorded: false };
function result<T>(data?: T, error?: Error) { return { data, error, isError: !!error, isRefetching: false, isFetching: false, refetch: jest.fn(async () => ({ data, isSuccess: true })) }; }
const mutate = jest.fn<ReturnType<typeof useOccurrenceMutation>['mutateAsync']>();
const calendar = { year: 2026, month: 10, context: summary.context, days: [{ date: projected.dueDate, paymentCount: 1, totalsByCurrency: [], payments: [projected] }] };
const currency: CurrencyAnalytics = { currencyCode: 'TRY', months: [{ month: '2026-10-01', plannedAmount: '899.9000', paidAmount: '40.1234', pendingAmount: '859.7766', overdueAmount: '0', paidCount: 1, pendingCount: 1, skippedCount: 1 }], paidCategories: [{ label: 'Ev', amount: '40.1234', count: 1 }], topPlans: [{ label: plan.name, amount: plan.amount, count: 1 }] };
beforeEach(() => {
  jest.mocked(useCalendar).mockReturnValue(result(calendar) as unknown as ReturnType<typeof useCalendar>);
  jest.mocked(useHistory).mockReturnValue(result({ items: [projected], context: summary.context, totalCount: 25, totalPages: 2, pageNumber: 1, pageSize: 20 }) as unknown as ReturnType<typeof useHistory>);
  jest.mocked(useOccurrenceMutation).mockReturnValue({ mutateAsync: mutate, isPending: false } as unknown as ReturnType<typeof useOccurrenceMutation>);
  jest.mocked(useAnalytics).mockReturnValue(result({ context: summary.context, currencies: [currency, { ...currency, currencyCode: 'USD' }] }) as unknown as ReturnType<typeof useAnalytics>);
  mutate.mockResolvedValue({ ...occurrence, status: 'Paid', actualAmount: '899.9000', paidAtUtc: '2026-10-23T12:00:00Z', isRecorded: true, rowVersion: plan.rowVersion });
  jest.mocked(passwordResetApi.forgot).mockResolvedValue(undefined); jest.mocked(passwordResetApi.reset).mockResolvedValue(undefined);
});
test('calendar selects real backend day, navigates months and returns to server today', async () => {
  await render(<CalendarBody today={today} />); expect(screen.getByText('Bu gün için ödeme yok')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: '2026-10-31, Bekliyor' })); expect(screen.getByText(plan.name)).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'Sonraki ay' })); expect(useCalendar).toHaveBeenLastCalledWith(2026, 11); expect(screen.queryByText(plan.name)).toBeNull();
  await fireEvent.press(screen.getByRole('button', { name: 'Bugüne dön' })); expect(useCalendar).toHaveBeenLastCalledWith(2026, 10);
});
test('calendar exposes loading, error and retry without sample amounts', async () => {
  jest.mocked(useCalendar).mockReturnValue(result() as unknown as ReturnType<typeof useCalendar>);
  const view = await render(<CalendarBody today={today} />); expect(screen.getByRole('progressbar')).toBeTruthy();
  const failed = result(undefined, new ApiError('Takvim alınamadı.', 503)); jest.mocked(useCalendar).mockReturnValue(failed as unknown as ReturnType<typeof useCalendar>);
  await view.rerender(<CalendarBody today={today} />); await fireEvent.press(screen.getByRole('button', { name: 'Tekrar dene' })); expect(failed.refetch).toHaveBeenCalled();
});
test('calendar status markers and labels distinguish paid skipped and overdue', async () => {
  const data = { ...calendar, days: [{ ...calendar.days[0], date: today, payments: [{ ...projected, dueDate: today, status: 'Paid', actualAmount: '40', paidAtUtc: '2026-10-23T12:00:00Z' }, { ...projected, paymentPlanId: '22222222-2222-4222-8222-222222222222', dueDate: today, status: 'Skipped' }] }] };
  jest.mocked(useCalendar).mockReturnValue(result(data) as unknown as ReturnType<typeof useCalendar>);
  await render(<CalendarBody today={today} />); expect(screen.getByText('Ödendi')).toBeTruthy(); expect(screen.getByText('Atlandı')).toBeTruthy();
});
test('history clamps current period to today, paginates and resets page on status/month change', async () => {
  await render(<HistoryBody today={today} />); expect(useHistory).toHaveBeenLastCalledWith('2026-10-01', today, 'Paid', 1);
  await fireEvent.press(screen.getByRole('button', { name: 'Sonraki sayfa' })); expect(useHistory).toHaveBeenLastCalledWith('2026-10-01', today, 'Paid', 2);
  await fireEvent.press(screen.getByRole('button', { name: 'Atlandı' })); expect(useHistory).toHaveBeenLastCalledWith('2026-10-01', today, 'Skipped', 1);
  await fireEvent.press(screen.getByRole('button', { name: 'Önceki ay' })); expect(useHistory).toHaveBeenLastCalledWith('2026-09-01', '2026-09-30', 'Skipped', 1);
});
test('paid action sends exact actual amount and does not invent a paid date', async () => {
  await render(<OccurrenceBody plan={plan} initial={occurrence} today={today} reload={async () => {}} />);
  await fireEvent.changeText(screen.getByLabelText('Gerçek ödenen tutar'), '100,1234'); await fireEvent.press(screen.getByRole('button', { name: 'Ödendi olarak işaretle' }));
  expect(mutate).toHaveBeenCalledWith({ id: plan.id, date: occurrence.dueDate, status: 'Paid', rowVersion: null, amount: '100,1234' }); expect(await screen.findByText('Ödeme durumu sunucuda güncellendi.')).toBeTruthy();
});
test('skipped and pending actions use the recorded concurrency version', async () => {
  const recorded = { ...occurrence, status: 'Paid' as const, rowVersion: plan.rowVersion, isRecorded: true, actualAmount: '10' };
  await render(<OccurrenceBody plan={plan} initial={recorded} today={today} reload={async () => {}} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Atlandı olarak işaretle' })); expect(mutate).toHaveBeenCalledWith(expect.objectContaining({ status: 'Skipped', rowVersion: plan.rowVersion }));
  await fireEvent.press(screen.getByRole('button', { name: 'Yeniden bekliyor' })); expect(mutate).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Pending', rowVersion: plan.rowVersion }));
});
test('occurrence conflict blocks writes until explicitly reloaded', async () => {
  mutate.mockRejectedValueOnce(new ApiError('Kayıt değişti.', 409)); const reload = jest.fn(async () => {});
  await render(<OccurrenceBody plan={plan} initial={occurrence} today={today} reload={reload} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Atlandı olarak işaretle' })); expect(await screen.findByText('Kayıt değişti.')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'Ödendi olarak işaretle' })); expect(mutate).toHaveBeenCalledTimes(1);
  await fireEvent.press(screen.getByRole('button', { name: 'Güncel durumu yükle' })); expect(reload).toHaveBeenCalled();
});
test('occurrence double submission is gated while the request is unresolved', async () => {
  let finish!: (value: Occurrence) => void; mutate.mockReturnValueOnce(new Promise(resolve => { finish = resolve; }));
  await render(<OccurrenceBody plan={plan} initial={occurrence} today={today} reload={async () => {}} />);
  await fireEvent.press(screen.getByRole('button', { name: 'Ödendi olarak işaretle' })); await fireEvent.press(screen.getByRole('button', { name: 'Atlandı olarak işaretle' })); expect(mutate).toHaveBeenCalledTimes(1);
  await act(async () => finish(occurrence));
});
test('native date picker opens with current date, cancels without mutation and preserves leap day', async () => {
  const change = jest.fn(); await render(<DateField label="Başlangıç" value="2024-02-29" onChange={change} fallback={today} />);
  await fireEvent.press(screen.getByRole('button', { name: /Başlangıç:/ }));
  let props = jest.mocked(DateTimePicker).mock.calls.at(-1)![0]; expect(calendarDateKey(props.value)).toBe('2024-02-29');
  await act(async () => props.onDismiss?.()); expect(change).not.toHaveBeenCalled();
  await fireEvent.press(screen.getByRole('button', { name: /Başlangıç:/ })); props = jest.mocked(DateTimePicker).mock.calls.at(-1)![0];
  await act(async () => props.onValueChange?.({ nativeEvent: { timestamp: 0, utcOffset: 0 } }, localCalendarDate('2024-03-31'))); expect(change).toHaveBeenCalledWith('2024-03-31');
});
test('optional end date can be cleared and its minimum follows the start date', async () => {
  const change = jest.fn(); await render(<DateField label="Bitiş" value="2026-10-01" onChange={change} fallback={today} minimum={today} optional />);
  await fireEvent.press(screen.getByRole('button', { name: /Bitiş:/ })); const props = jest.mocked(DateTimePicker).mock.calls.at(-1)![0]; expect(calendarDateKey(props.value)).toBe(today); expect(calendarDateKey(props.minimumDate!)).toBe(today);
  await fireEvent.press(screen.getByRole('button', { name: 'Bitiş tarihi yok' })); expect(change).toHaveBeenCalledWith('');
});
test('DateOnly roundtrips do not use UTC conversion including years under 100', () => {
  for (const key of ['2024-02-29', '2026-01-31', '0099-12-31']) expect(calendarDateKey(localCalendarDate(key))).toBe(key);
});
test('forgot form validates email and displays only the server generic response', async () => {
  await render(<PasswordResetScreen />); await fireEvent.press(screen.getByRole('button', { name: 'Yenileme bağlantısı iste' })); expect(passwordResetApi.forgot).not.toHaveBeenCalled();
  await fireEvent.changeText(screen.getByLabelText('E-posta'), 'test@example.com'); await fireEvent.press(screen.getByRole('button', { name: 'Yenileme bağlantısı iste' })); expect(passwordResetApi.forgot).toHaveBeenCalledWith('test@example.com'); expect(await screen.findByText(/Adres kayıtlıysa/)).toBeTruthy();
});
async function fillReset() { await fireEvent.changeText(screen.getByLabelText('E-postadaki bağlantı veya kod'), `exp://127.0.0.1:8085/--/reset-password?token=${'a'.repeat(43)}`); await fireEvent.changeText(screen.getByLabelText('Yeni şifre'), 'new-password-long!'); await fireEvent.changeText(screen.getByLabelText('Yeni şifre tekrar'), 'new-password-long!'); }
test('reset accepts email link fallback, clears secrets and invalidates the local session', async () => {
  await render(<PasswordResetScreen reset />); await fillReset(); await fireEvent.press(screen.getByRole('button', { name: 'Şifreyi yenile' })); expect(passwordResetApi.reset).toHaveBeenCalledWith('a'.repeat(43), 'new-password-long!'); expect(session.invalidate).toHaveBeenCalled(); expect(router.setParams).toHaveBeenCalledWith({ token: undefined }); expect(screen.queryByLabelText('Yeni şifre')).toBeNull();
});
test('reset invalid/expired token never shows success', async () => {
  jest.mocked(passwordResetApi.reset).mockRejectedValueOnce(new ApiError('Bağlantı geçersiz veya süresi dolmuş.', 400)); await render(<PasswordResetScreen reset />); await fillReset(); await fireEvent.press(screen.getByRole('button', { name: 'Şifreyi yenile' })); expect(await screen.findByText('Bağlantı geçersiz veya süresi dolmuş.')).toBeTruthy(); expect(screen.queryByText('Şifren yenilendi. Yeni şifrenle giriş yap.')).toBeNull();
});
test('reset rejects weak passwords, mismatch and malformed tokens locally', async () => {
  await render(<PasswordResetScreen reset />); await fireEvent.press(screen.getByRole('button', { name: 'Şifreyi yenile' })); expect(passwordResetApi.reset).not.toHaveBeenCalled();
  await fillReset(); await fireEvent.changeText(screen.getByLabelText('Yeni şifre tekrar'), 'different-password!'); await fireEvent.press(screen.getByRole('button', { name: 'Şifreyi yenile' })); expect(screen.getByText('Şifreler aynı olmalı.')).toBeTruthy(); expect(passwordResetApi.reset).not.toHaveBeenCalled();
});
test('reset link parser ignores malformed and duplicate-sized token values', () => { expect(extractResetToken('https://example.test/reset?token=bad')).toBe(''); expect(extractResetToken('a'.repeat(43))).toBe('a'.repeat(43)); expect(extractResetToken('not a url')).toBe(''); });
test('analytics separates currencies and labels actual versus planned totals', async () => {
  await render(<AnalyticsBody today={today} />); expect(screen.getByText('Gerçek ödenen · seçili ay')).toBeTruthy(); expect(screen.getByText('Planlanan · seçili ay')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'USD' })); expect(screen.getByRole('button', { name: 'USD ✓' })).toBeTruthy(); expect(screen.getByText(/Para birimleri ayrı tutulur/)).toBeTruthy();
});
test('analytics handles loading empty and failure without made-up totals', async () => {
  jest.mocked(useAnalytics).mockReturnValue(result() as unknown as ReturnType<typeof useAnalytics>); const view = await render(<AnalyticsBody today={today} />); expect(screen.getByRole('progressbar')).toBeTruthy();
  jest.mocked(useAnalytics).mockReturnValue(result({ context: summary.context, currencies: [] }) as unknown as ReturnType<typeof useAnalytics>); await view.rerender(<AnalyticsBody today={today} />); expect(screen.getByText('Analizlerin yeni planlara hazır')).toBeTruthy();
  const failed = result(undefined, new ApiError('Analiz alınamadı.', 503)); jest.mocked(useAnalytics).mockReturnValue(failed as unknown as ReturnType<typeof useAnalytics>); await view.rerender(<AnalyticsBody today={today} />); await fireEvent.press(screen.getByRole('button', { name: 'Tekrar dene' })); expect(failed.refetch).toHaveBeenCalled();
});
test('analytics lets a limited or failed query be narrowed by a validated server currency filter', async () => {
  await render(<AnalyticsBody today={today} />);
  await fireEvent.changeText(screen.getByLabelText('Para birimi filtresi · isteğe bağlı'), 'usd');
  await fireEvent.press(screen.getByRole('button', { name: 'Para birimi filtresini uygula' }));
  expect(useAnalytics).toHaveBeenLastCalledWith(2026, 10, 'USD');
  await fireEvent.changeText(screen.getByLabelText('Para birimi filtresi · isteğe bağlı'), 'ZZZ');
  await fireEvent.press(screen.getByRole('button', { name: 'Para birimi filtresini uygula' })); expect(screen.getByText('Geçerli ISO 4217 kodu gir.')).toBeTruthy();
});
test('chart ratios handle zero and maximum precision values without altering textual amounts', () => { expect(barRatios([{ label: 'empty', amount: '0' }])).toEqual([0]); expect(barRatios([{ label: 'max', amount: '999999999999999.9999' }, { label: 'half', amount: '500000000000000' }])[0]).toBe(1); });
