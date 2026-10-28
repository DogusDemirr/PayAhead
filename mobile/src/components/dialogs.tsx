import { useEffect, useRef, useState, type ReactNode } from 'react';
import { AccessibilityInfo, Keyboard, Modal, Pressable, ScrollView, View } from 'react-native';
import { CheckCircle2, Info, TriangleAlert, X } from 'lucide-react-native';
import { AppButton, AppText, IconButton } from './ui';
import { useTheme } from '../theme/use-theme';
import { isRTL, t, useLocaleRevision } from '../i18n/runtime';
import { ApiError } from '../services/api-error';
import { isCancel } from 'axios';

export type DialogButton = { text?: string; style?: 'cancel' | 'destructive' | 'default'; onPress?(): void | Promise<unknown> };
type Props = { visible: boolean; title: string; message: string; confirmLabel?: string; cancelLabel?: string; loading?: boolean; onConfirm(): void; onCancel(): void; destructive?: boolean; single?: boolean };
export function DialogFrame({ visible, title, onClose, busy = false, children, danger = false }: { visible: boolean; title: string; onClose(): void; busy?: boolean; children: ReactNode; danger?: boolean }) {
  useLocaleRevision(); const { colors } = useTheme(); const titleRef = useRef<View>(null);
  return <Modal testID="app-dialog" visible={visible} transparent animationType="fade" statusBarTranslucent onRequestClose={() => { if (!busy) onClose(); }} onShow={() => { Keyboard.dismiss(); AccessibilityInfo.announceForAccessibility(title); }}>
    <View style={{ flex: 1, backgroundColor: '#00000088', justifyContent: 'center', alignItems: 'center', paddingHorizontal: 20, paddingVertical: 48, direction: isRTL() ? 'rtl' : 'ltr' }}>
      <Pressable accessible={false} style={{ position: 'absolute', inset: 0 }} onPress={() => { if (!busy) onClose(); }} />
      <View ref={titleRef} accessibilityViewIsModal importantForAccessibility="yes" style={{ width: '100%', maxWidth: 420, maxHeight: '100%', borderRadius: 28, backgroundColor: colors.surface, padding: 24, gap: 20 }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}><View style={{ padding: 12, borderRadius: 18, backgroundColor: danger ? colors.dangerSoft : colors.accentSoft }}>{danger ? <TriangleAlert size={25} color={colors.danger} /> : <Info size={25} color={colors.accentText} />}</View><AppText accessibilityRole="header" variant="section" style={{ flex: 1 }}>{title}</AppText>{!busy && <IconButton icon={X} label={t('common:dialog.close')} onPress={onClose} />}</View>
        <ScrollView keyboardShouldPersistTaps="handled" contentContainerStyle={{ gap: 16, flexGrow: 1 }} showsVerticalScrollIndicator>{children}</ScrollView>
      </View>
    </View>
  </Modal>;
}
export function ConfirmDialog(p: Props) {
  useLocaleRevision(); return <DialogFrame visible={p.visible} title={p.title} onClose={p.onCancel} busy={p.loading} danger={p.destructive}><AppText>{p.message}</AppText><AppButton title={p.confirmLabel ?? t('common:dialog.confirm')} variant={p.destructive ? 'destructive' : 'primary'} loading={p.loading} onPress={p.onConfirm} />{!p.single && <AppButton title={p.cancelLabel ?? t('common:cancel')} variant="ghost" disabled={p.loading} onPress={p.onCancel} />}</DialogFrame>;
}
export function DestructiveConfirmDialog(p: Props) { return <ConfirmDialog {...p} destructive />; }
export function InfoDialog(p: Omit<Props, 'confirmLabel' | 'title'> & { title?: string }) { return <ConfirmDialog {...p} title={p.title ?? t('common:dialog.info')} single confirmLabel={t('common:dialog.close')} />; }
export function ErrorDialog(p: Omit<Props, 'confirmLabel'>) { return <ConfirmDialog {...p} single destructive confirmLabel={t('common:dialog.close')} />; }
export function SuccessFeedback({ message = t('common:dialog.success') }: { message?: string }) { useLocaleRevision(); const { colors } = useTheme(); return <View style={{ flexDirection: 'row', gap: 8, alignItems: 'center', paddingVertical: 12 }}><CheckCircle2 color={colors.success} size={20} /><AppText accessibilityRole="alert" accessibilityLiveRegion="polite" tone="success" style={{ flex: 1 }}>{message}</AppText></View>; }
export function dialogError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.code === 'payment_linked_subscription') return t('common:dialog.linked');
    if (error.code === 'payment_delete_related') return t('common:dialog.related');
    if (error.code === 'payment_changed') return t('common:dialog.changed');
    if (error.code === 'avatar_permission') return t('settings:avatar.permission');
    if (error.code === 'avatar_failed') return t('settings:avatar.failed');
    if (error.code === 'avatar_invalid') return t('settings:avatar.invalid');
    if (error.code === 'avatar_too_large' || error.status === 413) return t('settings:avatar.large');
    if (error.code === 'avatar_unavailable') return t('settings:avatar.unavailable');
    if (error.code === 'avatar_changed') return t('settings:profile.uncertainWrite');
    if (error.kind === 'network' || error.kind === 'timeout') return t('common:dialog.network');
  }
  return t('common:dialog.failed');
}
export function useAppDialog() {
  useLocaleRevision(); const [value, setValue] = useState<{ title: string; message: string; buttons: DialogButton[]; error?: boolean } | null>(null);
  const [pending, setPending] = useState(false); const busy = useRef(false); const mounted = useRef(true);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  const close = () => { if (!busy.current) setValue(null); };
  async function run(button?: DialogButton) {
    if (busy.current) return; busy.current = true; setPending(true);
    try { await button?.onPress?.(); if (mounted.current) setValue(null); }
    catch (e) { if (mounted.current) setValue(isCancel(e) ? null : { title: t('common:dialog.error'), message: dialogError(e), buttons: [{ text: t('common:dialog.close') }], error: true }); }
    finally { busy.current = false; if (mounted.current) setPending(false); }
  }
  const cancel = value?.buttons.find(x => x.style === 'cancel'); const action = value?.buttons.find(x => x.style !== 'cancel');
  const Component = value?.error ? ErrorDialog : action?.style === 'destructive' ? DestructiveConfirmDialog : ConfirmDialog;
  return {
    alert: (title: string, message: string, buttons: DialogButton[]) => { if (!busy.current) setValue({ title, message, buttons }); },
    error: (error: unknown) => { if (!busy.current) setValue({ title: t('common:dialog.error'), message: dialogError(error), buttons: [{ text: t('common:dialog.close') }], error: true }); },
    info: (title: string, message: string) => { if (!busy.current) setValue({ title, message, buttons: [{ text: t('common:dialog.close') }] }); },
    dialog: value && <Component visible title={value.title} message={value.message} confirmLabel={action?.text} cancelLabel={cancel?.text} single={!cancel} loading={pending} onConfirm={() => { void run(action); }} onCancel={() => { if (!busy.current) { cancel?.onPress?.(); close(); } }} />,
  };
}
