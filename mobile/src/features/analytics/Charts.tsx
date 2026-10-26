import { t } from "../../i18n/runtime";
import { memo } from 'react';
import { View } from 'react-native';
import Svg, { Rect, Circle, Line } from 'react-native-svg';
import { AppText } from '../../components/ui';
import { financialMoney } from '../payments/money';
import { useTheme } from '../../theme/use-theme';
export interface ChartBar {
    label: string;
    amount: string;
}
// Number conversion is exclusively for pixel ratios; totals and text remain exact decimal strings.
export function barRatios(rows: ChartBar[]) { const values = rows.map(x => Math.max(0, Number(x.amount))); const max = Math.max(0, ...values); return values.map(v => max ? v / max : 0); }
export const AmountChart = memo(function AmountChart({ rows, currency }: {
    rows: ChartBar[];
    currency: string;
}) {
    const { colors } = useTheme();
    const ratios = barRatios(rows);
    return <View><Svg accessible={false} width="100%" height={170} viewBox="0 0 300 170"><Line x1={0} x2={300} y1={155} y2={155} stroke={colors.border}/>{rows.map((r, i) => <Rect key={`${r.label}-${i}`} x={i * (300 / rows.length) + 8} y={155 - ratios[i] * 140} width={Math.max(4, 300 / rows.length - 16)} height={ratios[i] * 140} rx={5} fill={colors.accentText}/>)}</Svg><View style={{ gap: 8 }}>{rows.map((r, i) => <View key={`${r.label}-${i}`} style={{ flexDirection: 'row', justifyContent: 'space-between', gap: 8 }}><AppText variant="caption" tone="muted" style={{ flex: 1 }}>{r.label}</AppText><AppText variant="caption">{financialMoney(r.amount, currency)}</AppText></View>)}</View></View>;
});
export const StatusChart = memo(function StatusChart({ paid, pending, skipped }: {
    paid: number;
    pending: number;
    skipped: number;
}) {
    const { colors } = useTheme();
    const rows = [{ label: t("analytics:m_2f36f3362ead"), count: paid, color: colors.success }, { label: t("common:m_9025b4af025a"), count: pending, color: colors.accentText }, { label: t("analytics:m_594b1254c47d"), count: skipped, color: colors.muted }];
    const total = paid + pending + skipped;
    let offset = 0;
    return <View style={{ gap: 12 }}><Svg accessible={false} width="100%" height={150} viewBox="0 0 150 150"><Circle cx={75} cy={75} r={54} fill="none" stroke={colors.border} strokeWidth={20}/>{rows.map(row => { const fraction = total ? row.count / total : 0; const start = offset; offset += fraction; return <Circle key={row.label} cx={75} cy={75} r={54} fill="none" stroke={row.color} strokeWidth={20} strokeDasharray={`${fraction * 339.292} 339.292`} strokeDashoffset={-start * 339.292} rotation={-90} origin="75,75"/>; })}</Svg>{rows.map(row => <AppText key={row.label} variant="caption">{t("analytics:m_e7e0215652f9", { value0: row.label, value1: row.count })}</AppText>)}</View>;
});
