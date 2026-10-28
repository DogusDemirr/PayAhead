import { afterEach, expect, jest, test } from '@jest/globals';
import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { AppButton } from '../src/components/ui';
import { ConfirmDialog, ErrorDialog, InfoDialog, SuccessFeedback, useAppDialog } from '../src/components/dialogs';
import { ApiError } from '../src/services/api-error';
import { i18next, t, useLanguageStore } from '../src/i18n/runtime';
import { languages } from '../src/i18n/languages';
import { useUiStore } from '../src/store/ui';

afterEach(() => { void i18next.changeLanguage('tr'); useLanguageStore.setState({ language: 'tr' }); useUiStore.setState({ theme: 'system' }); });
test.each(languages)('$code has translated, RTL-aware destructive confirmation', async ({ code, rtl }) => {
  await act(async () => { await i18next.changeLanguage(code); useLanguageStore.setState({ language: code }); });
  await render(<ConfirmDialog visible destructive title={t('common:dialog.confirm')} message={t('common:dialog.related')} onConfirm={() => {}} onCancel={() => {}} />);
  expect(screen.getByRole('header', { name: t('common:dialog.confirm') })).toBeTruthy();
  expect(screen.getByRole('button', { name: t('common:cancel') })).toBeTruthy();
  expect(screen.getByText(t('common:dialog.related'))).toBeTruthy();
  expect(JSON.stringify(screen.toJSON())).toContain('"direction":"' + (rtl ? 'rtl' : 'ltr') + '"');
});
test.each(['light', 'dark'] as const)('%s dialog cancels on Android back and blocks dismissal while pending', async theme => {
  useUiStore.setState({ theme }); const cancel = jest.fn();
  const view = await render(<ConfirmDialog visible title="Test" message={'Long '.repeat(300)} onConfirm={() => {}} onCancel={cancel} />);
  await fireEvent(screen.getByTestId('app-dialog'), 'requestClose'); expect(cancel).toHaveBeenCalledTimes(1);
  await view.rerender(<ConfirmDialog visible loading title="Test" message="Busy" onConfirm={() => {}} onCancel={cancel} />);
  await fireEvent(screen.getByTestId('app-dialog'), 'requestClose'); expect(cancel).toHaveBeenCalledTimes(1);
  expect(screen.getByRole('button', { name: t('common:cancel') })).toBeDisabled();
});
function Harness({ run }: { run(): Promise<unknown> }) {
  const dialog = useAppDialog();
  return <><AppButton title="Open" onPress={() => dialog.alert('Delete', 'Confirm once', [{ text: 'Cancel', style: 'cancel' }, { text: 'Delete now', style: 'destructive', onPress: run }])} />{dialog.dialog}</>;
}
test('one confirmation sends once, remains busy and presents a safe business error', async () => {
  let reject!: (e: unknown) => void; const run = jest.fn(() => new Promise((_, fail) => { reject = fail; }));
  await render(<Harness run={run} />); await fireEvent.press(screen.getByRole('button', { name: 'Open' }));
  const button = screen.getByRole('button', { name: 'Delete now' }); await fireEvent.press(button); await fireEvent.press(button); expect(run).toHaveBeenCalledTimes(1);
  await act(async () => reject(new ApiError('private detail', 409, undefined, undefined, 'http', 'payment_linked_subscription')));
  expect(await screen.findByText(t('common:dialog.linked'))).toBeTruthy(); expect(screen.queryByText('private detail')).toBeNull();
  await fireEvent.press(screen.getAllByRole('button', { name: t('common:dialog.close') }).at(-1)!); expect(screen.queryByText(t('common:dialog.linked'))).toBeNull();
});
test('cancel does not run a destructive action', async () => {
  const run = jest.fn(async () => undefined); await render(<Harness run={run} />); await fireEvent.press(screen.getByRole('button', { name: 'Open' })); await fireEvent.press(screen.getByRole('button', { name: 'Cancel' })); expect(run).not.toHaveBeenCalled();
});
test('information, error and inline success remain accessible', async () => {
  const view = await render(<InfoDialog visible title={t('common:dialog.info')} message="Information" onConfirm={() => {}} onCancel={() => {}} />); expect(screen.getByText('Information')).toBeTruthy();
  await view.rerender(<ErrorDialog visible title={t('common:dialog.error')} message="Error" onConfirm={() => {}} onCancel={() => {}} />); expect(screen.getByText('Error')).toBeTruthy();
  await view.rerender(<SuccessFeedback message={t('common:dialog.success')} />); expect(screen.getByRole('alert')).toBeTruthy();
});
