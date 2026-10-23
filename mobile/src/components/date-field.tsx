import { useState } from 'react';
import { Platform, View } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import { AppButton, AppInput, AppText } from './ui';
import { dayKey, validDate } from '../features/payments/dates';
import { dateLabel } from '../utils/format';
import { useTheme } from '../theme/use-theme';

export function localCalendarDate(value: string) {
  const [y, m, d] = value.split('-').map(Number); const date = new Date(0);
  date.setFullYear(y, m - 1, d); date.setHours(12, 0, 0, 0); return date;
}
export function calendarDateKey(date: Date) { return dayKey(date.getFullYear(), date.getMonth() + 1, date.getDate()); }
export function DateField({ label, value, onChange, fallback, minimum, optional, disabled, error }: { label: string; value: string; onChange: (value: string) => void; fallback: string; minimum?: string; optional?: boolean; disabled?: boolean; error?: string }) {
  const [open, setOpen] = useState(false); const { scheme } = useTheme();
  const initial = validDate(value) ? value : validDate(fallback) ? fallback : '2000-01-01';
  const selected = minimum && initial < minimum ? minimum : initial;
  return <View style={{ gap: 8 }}><AppText variant="label">{label}</AppText>
    {Platform.OS === 'web' ? <AppInput label={`${label} · YYYY-AA-GG`} value={value} onChangeText={onChange} editable={!disabled} maxLength={10} /> : <AppButton title={`${label}: ${value && validDate(value) ? dateLabel(value, true, true) : 'Tarih seç'}`} variant="secondary" disabled={disabled} onPress={() => setOpen(true)} />}
    {optional && <AppButton title="Bitiş tarihi yok" variant="ghost" disabled={disabled || !value} onPress={() => { setOpen(false); onChange(''); }} />}
    {error && <AppText tone="danger" accessibilityRole="alert">{error}</AppText>}
    {open && <><DateTimePicker value={localCalendarDate(selected)} minimumDate={minimum && validDate(minimum) ? localCalendarDate(minimum) : localCalendarDate('0001-01-01')} maximumDate={localCalendarDate('9999-12-31')} mode="date" themeVariant={scheme === 'dark' ? 'dark' : 'light'} onDismiss={() => setOpen(false)} onValueChange={(_, date) => { onChange(calendarDateKey(date)); setOpen(false); }} />{Platform.OS === 'ios' && <AppButton title="İptal" variant="ghost" onPress={() => setOpen(false)} />}</>}
  </View>;
}
