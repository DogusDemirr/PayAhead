import { test, expect, jest, beforeEach, afterEach } from '@jest/globals';
import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { CatalogPicker, CatalogSelectionField } from '../src/features/catalog/CatalogPicker';
import { CatalogIcon, CatalogIdentity } from '../src/features/catalog/CatalogIdentity';
import { useCatalogCategories, useCatalogServices, useCatalogService } from '../src/features/catalog/queries';
import { normalizeCatalogSearch, type CatalogFilter } from '../src/features/catalog/contracts';
import { PaymentForm } from '../src/features/payments/PaymentFormScreen';
import { SubscriptionForm } from '../src/features/subscriptions/SubscriptionFormScreen';
import { usePaymentMutation } from '../src/features/payments/queries';
import { useSubscriptionMutation } from '../src/features/subscriptions/queries';
import { useUiStore } from '../src/store/ui';
import { categories, netflix, shell, transport, petrol, other, video } from './catalog-fixtures';
import { plan } from './finance-fixtures';
import treeFixture from './catalog-tree.fixture.json';
import logoManifest from '../assets/brands/manifest.json';
import { categorySchema } from '../src/features/catalog/contracts';
jest.mock('../src/features/catalog/queries', () => ({ useCatalogCategories: jest.fn(), useCatalogServices: jest.fn(), useCatalogService: jest.fn() }));
jest.mock('../src/features/payments/queries', () => ({ usePaymentMutation: jest.fn(), useFinanceClock: jest.fn(), usePayment: jest.fn(), useEntitlements: jest.fn() }));
jest.mock('../src/features/subscriptions/queries', () => ({ useSubscriptionMutation: jest.fn(), useSubscription: jest.fn(), useSubscriptionSummary: jest.fn() }));
const payment = jest.fn<ReturnType<typeof usePaymentMutation>['mutateAsync']>(); const subscription = jest.fn<ReturnType<typeof useSubscriptionMutation>['mutateAsync']>();
function result<T>(data?: T, isError = false) { return { data, isError, refetch: jest.fn(async () => ({})), isFetching: false }; }
beforeEach(() => {
  jest.mocked(useCatalogCategories).mockReturnValue(result(categories) as unknown as ReturnType<typeof useCatalogCategories>);
  jest.mocked(useCatalogServices).mockImplementation((filter: CatalogFilter) => { const items = filter.categoryId === petrol.id ? [shell] : filter.categoryId ? [] : [netflix]; return result({ items, totalCount: items.length, totalPages: 1, pageNumber: filter.pageNumber, pageSize: 20 }) as unknown as ReturnType<typeof useCatalogServices>; });
  jest.mocked(useCatalogService).mockReturnValue(result(netflix) as unknown as ReturnType<typeof useCatalogService>);
  payment.mockReset(); subscription.mockReset(); payment.mockResolvedValue(plan); subscription.mockResolvedValue(undefined);
  jest.mocked(usePaymentMutation).mockReturnValue({ mutateAsync: payment, isPending: false } as unknown as ReturnType<typeof usePaymentMutation>); jest.mocked(useSubscriptionMutation).mockReturnValue({ mutateAsync: subscription, isPending: false } as unknown as ReturnType<typeof useSubscriptionMutation>);
});
afterEach(() => { jest.useRealTimers(); useUiStore.setState({ theme: 'system' }); });
test('popular service selection returns immutable category/service records and subscription filters exclude fuel', async () => {
  const select = jest.fn(); await render(<CatalogPicker mode="Subscription" onSelect={select} onClose={() => {}} />); expect(screen.queryByRole('button', { name: 'Araç ve Ulaşım' })).toBeNull(); await fireEvent.press(screen.getByRole('button', { name: 'Netflix' })); expect(select).toHaveBeenCalledWith({ category: video, service: netflix }); expect(useCatalogServices).toHaveBeenCalledWith(expect.objectContaining({ applicableTo: 'Subscription', popular: true, countryCode: 'TR' }));
});
test('transport fuel hierarchy selects Shell as one-off payment without creating subscription', async () => {
  const select = jest.fn(); await render(<CatalogPicker mode="Payment" onSelect={select} onClose={() => {}} />);
  for (const name of ['Araç ve Ulaşım', 'Yakıt', 'Benzin']) await fireEvent.press(screen.getByRole('button', { name })); await fireEvent.press(screen.getByRole('button', { name: 'Shell' })); expect(select).toHaveBeenCalledWith({ category: petrol, service: shell }); expect(subscription).not.toHaveBeenCalled();
});
test('the whole category card opens children and search selection reconstructs real back navigation', async () => {
  await render(<CatalogPicker mode="Payment" onSelect={() => {}} onClose={() => {}} />);
  await fireEvent.press(screen.getByTestId('catalog-card-transport'));
  expect(screen.getByRole('button', { name: 'Yakıt' })).toBeTruthy();
  expect(screen.getByText('Seçili yol: Araç ve Ulaşım')).toBeTruthy();
  await fireEvent.press(screen.getByRole('button', { name: 'Ana kategorilere dön' }));
  expect(screen.getByTestId('catalog-card-transport')).toBeTruthy();
});
test('real seeded fuel path selects Shell directly from Yakıt with a valid category membership', async () => {
  const tree = treeFixture.filter(c => c.code === 'transport' || c.code.startsWith('transport.')).map(c => categorySchema.parse(c)); const category = tree.find(c => c.code === 'transport.fuel')!;
  const fuelShell = { ...shell, categoryId: tree.find(c => c.code === 'transport.petrol')!.id, categoryIds: [category.id] };
  jest.mocked(useCatalogCategories).mockReturnValue(result(tree) as unknown as ReturnType<typeof useCatalogCategories>);
  jest.mocked(useCatalogServices).mockReturnValue(result({ items: [fuelShell], totalCount: 1, totalPages: 1, pageNumber: 1, pageSize: 20 }) as unknown as ReturnType<typeof useCatalogServices>);
  const select = jest.fn(); await render(<CatalogPicker mode="Payment" onSelect={select} onClose={() => {}} />);
  await fireEvent.press(screen.getByTestId('catalog-card-transport')); await fireEvent.press(screen.getByTestId('catalog-card-transport.fuel'));
  await fireEvent.press(screen.getByRole('button', { name: 'Shell' })); expect(select).toHaveBeenCalledWith({ category, service: fuelShell });
});
test('news digital newspaper selects a publisher and an empty print category offers custom subscription', async () => {
  const tree = treeFixture.filter(c => c.code === 'news' || c.code.startsWith('news.')).map(c => categorySchema.parse(c)); const digital = tree.find(c => c.code === 'news.digital-newspaper')!;
  const paper = { ...netflix, code: 'new-york-times', displayName: 'The New York Times', categoryId: digital.id, categoryIds: [digital.id], defaultRecurrenceType: null };
  jest.mocked(useCatalogCategories).mockReturnValue(result(tree) as unknown as ReturnType<typeof useCatalogCategories>);
  jest.mocked(useCatalogServices).mockImplementation(filter => result({ items: filter.categoryId === digital.id ? [paper] : [], totalCount: filter.categoryId === digital.id ? 1 : 0, totalPages: 1, pageNumber: 1, pageSize: 20 }) as unknown as ReturnType<typeof useCatalogServices>);
  const select = jest.fn(); await render(<CatalogPicker mode="Subscription" onSelect={select} onClose={() => {}} />);
  await fireEvent.press(screen.getByTestId('catalog-card-news')); await fireEvent.press(screen.getByTestId('catalog-card-news.digital-newspaper')); await fireEvent.press(screen.getByRole('button', { name: 'The New York Times' }));
  expect(select).toHaveBeenCalledWith({ category: digital, service: paper });
  await fireEvent.press(screen.getByRole('button', { name: 'Seçimde geri dön' })); await fireEvent.press(screen.getByTestId('catalog-card-news.print-newspaper')); await fireEvent.press(screen.getByRole('button', { name: 'Bu kategoride özel abonelik' }));
  expect(select).toHaveBeenLastCalledWith({ category: tree.find(c => c.code === 'news.print-newspaper'), service: null });
});
test('registered Netflix asset renders an actual local logo rather than the fallback', async () => {
  const entry = logoManifest.find(x => x.code === 'netflix')!;
  await render(<CatalogIcon category={video} service={{ id: entry.serviceId, displayName: 'Netflix' }} />);
  expect(screen.getByLabelText('Netflix logosu')).toBeTruthy(); expect(screen.queryByLabelText('Film ve Dizi kategori ikonu')).toBeNull();
});
test('category custom entry preserves category and never adds private service to shared catalog', async () => {
  const select = jest.fn(); await render(<CatalogPicker mode="Payment" onSelect={select} onClose={() => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Araç ve Ulaşım' })); await fireEvent.press(screen.getByRole('button', { name: 'Bu kategoride özel ödeme' })); expect(select).toHaveBeenCalledWith({ category: transport, service: null });
});
test('Turkish variant search is debounced and requests server search without inventing results', async () => {
  jest.useFakeTimers(); await render(<CatalogPicker mode="Payment" onSelect={() => {}} onClose={() => {}} />); await fireEvent.changeText(screen.getByLabelText('Kategori veya hizmet ara'), 'arac'); await act(async () => { jest.advanceTimersByTime(350); }); expect(useCatalogServices).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'arac', popular: undefined })); expect(screen.getByRole('button', { name: 'Araç ve Ulaşım' })).toBeTruthy(); expect(normalizeCatalogSearch('İnternet DOĞALGAZ IŞIK')).toBe('internet dogalgaz isik');
});
test('empty search and retry do not fabricate brands, cached offline data stays selectable', async () => {
  jest.mocked(useCatalogServices).mockReturnValue(result({ items: [], totalCount: 0, totalPages: 0, pageNumber: 1, pageSize: 20 }, true) as unknown as ReturnType<typeof useCatalogServices>);
  await render(<CatalogPicker mode="Subscription" onSelect={() => {}} onClose={() => {}} />); expect(screen.getByText(/Katalog yenilenemedi/)).toBeTruthy(); await fireEvent.press(screen.getByRole('button', { name: 'Kataloğu yeniden dene' })); expect(useCatalogCategories().refetch).toHaveBeenCalled(); expect(screen.getByRole('button', { name: 'Diğer' })).toBeTruthy(); expect(screen.queryByRole('button', { name: 'Netflix' })).toBeNull();
});
test('Android modal back traverses category path before closing', async () => {
  await render(<CatalogSelectionField mode="Payment" initialOpen onSelect={() => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Araç ve Ulaşım' })); await fireEvent(screen.getByTestId('catalog-picker-modal'), 'requestClose'); expect(screen.getByRole('button', { name: 'Dijital Abonelikler' })).toBeTruthy(); await fireEvent(screen.getByTestId('catalog-picker-modal'), 'requestClose'); expect(screen.queryByRole('button', { name: 'Dijital Abonelikler' })).toBeNull(); expect(screen.getByRole('button', { name: 'Kategori ve hizmet seç' })).toBeTruthy();
});
test('logo absent or failed uses a theme-aware category icon and never a remote logo URL', async () => {
  const view = await render(<CatalogIcon category={video} service={netflix} source={1} />); await fireEvent(screen.getByLabelText('Netflix logosu'), 'error'); expect(screen.getByLabelText('Film ve Dizi kategori ikonu')).toBeTruthy(); await act(async () => useUiStore.setState({ theme: 'dark' })); await view.rerender(<CatalogIcon category={video} service={netflix} source={1} />); expect(screen.getByLabelText('Film ve Dizi kategori ikonu')).toBeTruthy();
});
test('legacy identity renders its original category without a catalog network dependency', async () => {
  await render(<CatalogIdentity fallback="Eski özel kategori" />); expect(screen.getByText('Eski özel kategori')).toBeTruthy(); expect(useCatalogCategories).not.toHaveBeenCalled(); expect(useCatalogService).not.toHaveBeenCalled();
});
test('Netflix payment prefill hides unnecessary naming inputs and sends one exact financial write', async () => {
  await render(<PaymentForm today="2026-10-25" activeCount={0} reload={async () => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Netflix' })); expect(screen.queryByLabelText('Ödeme adı')).toBeNull(); expect(screen.getByText('Netflix')).toBeTruthy(); await fireEvent.changeText(screen.getByLabelText('Tutar'), '999999999999999,9999'); await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' })); expect(payment).toHaveBeenCalledTimes(1); expect(payment).toHaveBeenCalledWith({ kind: 'create', body: expect.objectContaining({ catalogCategoryId: video.id, catalogServiceId: netflix.id, name: 'Netflix', category: 'Film ve Dizi', amount: '999999999999999.9999', frequency: 'Monthly' }) });
});
test('subscription service prefill sends same catalog refs via existing single-plan subscription endpoint', async () => {
  await render(<SubscriptionForm today="2026-10-25" trackingCount={0} paymentCount={0} reload={async () => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Netflix' })); expect(screen.queryByLabelText('Abonelik adı')).toBeNull(); expect(screen.queryByLabelText('Sağlayıcı adı')).toBeNull(); await fireEvent.changeText(screen.getByLabelText('Abonelik tutarı'), '199,9001'); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(subscription).toHaveBeenCalledWith({ kind: 'create', body: expect.objectContaining({ name: 'Netflix', providerName: 'Netflix', catalogCategoryId: video.id, catalogServiceId: netflix.id, amount: '199.9001' }) }); expect(payment).not.toHaveBeenCalled();
});
test('custom subscription and custom payment retain user naming with a catalog category only', async () => {
  const view = await render(<SubscriptionForm today="2026-10-25" trackingCount={0} paymentCount={0} reload={async () => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Özel Abonelik · Diğer' })); await fireEvent.changeText(screen.getByLabelText('Abonelik adı'), 'Özel stüdyo'); await fireEvent.changeText(screen.getByLabelText('Sağlayıcı adı'), 'Özel sağlayıcı'); await fireEvent.changeText(screen.getByLabelText('Abonelik tutarı'), '10'); await fireEvent.press(screen.getByRole('button', { name: 'Aboneliği oluştur' })); expect(subscription).toHaveBeenCalledWith(expect.objectContaining({ body: expect.objectContaining({ name: 'Özel stüdyo', catalogServiceId: null, catalogCategoryId: other.id }) })); await view.unmount();
  await render(<PaymentForm today="2026-10-25" activeCount={0} reload={async () => {}} />); await fireEvent.press(screen.getByRole('button', { name: 'Özel Ödeme · Diğer' })); await fireEvent.changeText(screen.getByLabelText('Ödeme adı'), 'Mahalle Tamircisi'); await fireEvent.changeText(screen.getByLabelText('Tutar'), '3000'); await fireEvent.press(screen.getByRole('button', { name: 'Ödeme planını oluştur' })); expect(payment).toHaveBeenCalledWith(expect.objectContaining({ body: expect.objectContaining({ name: 'Mahalle Tamircisi', catalogServiceId: null, catalogCategoryId: other.id }) }));
});
test('old edit keeps custom names and does not force a catalog brand association', async () => {
  await render(<PaymentForm plan={plan} today="2026-10-25" activeCount={1} reload={async () => {}} />); expect(screen.getByLabelText('Ödeme adı').props.value).toBe(plan.name); await fireEvent.press(screen.getByRole('button', { name: 'Değişiklikleri kaydet' })); expect(payment).toHaveBeenCalledWith(expect.objectContaining({ rowVersion: plan.rowVersion, body: expect.objectContaining({ name: plan.name, category: plan.category }) })); expect(payment.mock.calls[0][0]).not.toHaveProperty('body.catalogServiceId');
});
