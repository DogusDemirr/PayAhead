import { t, useLocaleRevision } from "../../i18n/runtime";
import { useState } from 'react';
import { Platform, View } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import { AppButton, AppInput, AppText } from '../../components/ui';
import { useTheme } from '../../theme/use-theme';
export function TimeField({ value, onChange, disabled, error }: {
    value: string;
    onChange(value: string): void;
    disabled?: boolean;
    error?: string;
}) {
    useLocaleRevision();
    const [open, setOpen] = useState(false);
    const { scheme } = useTheme();
    const parts = /^([01]\d|2[0-3]):[0-5]\d$/.test(value) ? value.split(':').map(Number) : [9, 0];
    const date = new Date();
    date.setHours(parts[0], parts[1], 0, 0);
    return <View style={{ gap: 8 }}><AppText variant="label">{t("reminders:m_4e2d3f27bc91")}</AppText>{Platform.OS === 'web' ? <AppInput label={t("reminders:m_bb7ed58f17c4")} value={value} onChangeText={onChange} editable={!disabled} maxLength={5}/> : <AppButton title={t("reminders:m_c62d351d4d79", { value0: value })} variant="secondary" disabled={disabled} onPress={() => setOpen(true)}/>}
    {error && <AppText tone="danger" accessibilityRole="alert">{error}</AppText>}{open && <><DateTimePicker value={date} mode="time" is24Hour themeVariant={scheme === 'dark' ? 'dark' : 'light'} onDismiss={() => setOpen(false)} onValueChange={(_, chosen) => { onChange(`${String(chosen.getHours()).padStart(2, '0')}:${String(chosen.getMinutes()).padStart(2, '0')}`); setOpen(false); }}/>{Platform.OS === 'ios' && <AppButton title={t("reminders:m_72278748133a")} variant="ghost" onPress={() => setOpen(false)}/>}</>}
  </View>;
}
