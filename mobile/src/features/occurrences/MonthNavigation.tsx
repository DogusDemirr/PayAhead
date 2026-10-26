import { currentLocale, t, useLocaleRevision, isRTL } from '../../i18n/runtime';
import { View } from 'react-native';
import { ChevronLeft, ChevronRight } from 'lucide-react-native';
import { AppText, IconButton, styles } from '../../components/ui';
import { dayKey } from '../payments/dates';
import { localCalendarDate } from '../../components/date-field';
export function shiftMonth(key: string, delta: number) { const [y, m] = key.split('-').map(Number); const n = y * 12 + m - 1 + delta; return dayKey(Math.floor(n / 12), n % 12 + 1, 1); }
export function MonthNavigation({ month, onChange, maximum }: {
    month: string;
    onChange: (month: string) => void;
    maximum?: string;
}) {
    useLocaleRevision();
    return <View style={[styles.row, { marginVertical: 12, gap: 4 }]}><View style={{ flex: 1 }}><AppText variant="section">{localCalendarDate(month).toLocaleDateString(currentLocale(), { month: 'long', year: 'numeric' })}</AppText></View><IconButton icon={isRTL() ? ChevronRight : ChevronLeft} label={t("calendar:m_140b6a85ed4f")} onPress={() => { if (month > '0001-01-01')
        onChange(shiftMonth(month, -1)); }}/><IconButton icon={isRTL() ? ChevronLeft : ChevronRight} label={t("calendar:m_bc1d7478bad2")} onPress={() => { const next = shiftMonth(month, 1); if (month < '9999-12-01' && (!maximum || next <= maximum))
        onChange(next); }}/></View>;
}
