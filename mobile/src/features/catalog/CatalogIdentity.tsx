import { useState } from 'react';
import { Image, View, type ImageSourcePropType } from 'react-native';
import { Clapperboard, Music2, Gamepad2, Sparkles, Laptop, Cloud, GraduationCap, Dumbbell, Wifi, Lightbulb, House, Car, ShieldCheck, Landmark, ShoppingBag, ShoppingBasket, HeartPulse, FileText, Newspaper, Users, Scissors, Plane, BriefcaseBusiness, Grid2X2, Fuel, Layers, type LucideIcon } from 'lucide-react-native';
import { AppText } from '../../components/ui';
import { useTheme } from '../../theme/use-theme';
import { useCatalogCategories, useCatalogService } from './queries';
import type { CatalogCategory, CatalogService } from './contracts';
import { localServiceLogos } from './brand-assets';
export { localServiceLogos } from './brand-assets';
const icons: Record<string, LucideIcon> = { digital: Layers, video: Clapperboard, music: Music2, gaming: Gamepad2, ai: Sparkles, software: Laptop, cloud: Cloud, education: GraduationCap, fitness: Dumbbell, telecom: Wifi, utilities: Lightbulb, home: House, transport: Car, insurance: ShieldCheck, finance: Landmark, shopping: ShoppingBag, groceries: ShoppingBasket, health: HeartPulse, tax: FileText, news: Newspaper, family: Users, personal: Scissors, travel: Plane, work: BriefcaseBusiness, fuel: Fuel, other: Grid2X2 };
export function CatalogIcon({ category, service, source, size = 42 }: { category?: Pick<CatalogCategory, 'iconKey' | 'nameTr'>; service?: Pick<CatalogService, 'id' | 'displayName'> | null; source?: ImageSourcePropType; size?: number }) {
  const { colors } = useTheme(); const [failed, setFailed] = useState(false); const image = source ?? (service ? localServiceLogos[service.id] : undefined); const Icon = icons[category?.iconKey ?? 'other'] ?? Grid2X2;
  return <View style={{ width: size, height: size, borderRadius: 13, backgroundColor: image && !failed ? colors.white : colors.accentSoft, alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>{image && !failed ? <Image source={image} accessibilityLabel={`${service?.displayName ?? 'Hizmet'} logosu`} resizeMode="contain" style={{ width: size - 10, height: size - 10 }} onError={() => setFailed(true)} /> : <Icon accessible accessibilityRole="image" aria-hidden={false} accessibilityLabel={`${category?.nameTr ?? 'Özel kayıt'} kategori ikonu`} size={size * .52} color={colors.accentText} />}</View>;
}
export function CatalogIdentity({ categoryId, serviceId, fallback, compact = false }: { categoryId?: string | null; serviceId?: string | null; fallback?: string; compact?: boolean }) {
  if (!categoryId && !serviceId) return <View style={{ flexDirection: 'row', gap: 10, alignItems: 'center' }}><CatalogIcon size={compact ? 32 : 42} /><AppText variant="caption" tone="muted">{fallback ?? 'Özel kayıt'}</AppText></View>;
  return <ResolvedIdentity categoryId={categoryId} serviceId={serviceId} fallback={fallback} compact={compact} />;
}
function ResolvedIdentity({ categoryId, serviceId, fallback, compact }: { categoryId?: string | null; serviceId?: string | null; fallback?: string; compact: boolean }) {
  const categories = useCatalogCategories(); const service = useCatalogService(serviceId); const category = categories.data?.find(x => x.id === categoryId);
  return <View style={{ flexDirection: 'row', gap: 10, alignItems: 'center' }}><CatalogIcon key={`${serviceId}-${categoryId}`} category={category} service={service.data} size={compact ? 32 : 42} /><View style={{ flex: 1 }}>{service.data && <AppText variant="caption" tone="muted">{service.data.displayName}{!service.data.isActive ? ' · Katalogda pasif' : ''}</AppText>}<AppText variant="caption" tone="muted">{category?.nameTr ?? fallback ?? 'Özel kayıt'}</AppText></View></View>;
}
