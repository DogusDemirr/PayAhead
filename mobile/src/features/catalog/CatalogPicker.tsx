import { t, useLocaleRevision, isRTL } from "../../i18n/runtime";
import { useCallback, useEffect, useRef, useState, type RefObject } from 'react';
import { FlatList, Modal, Pressable, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { AppButton, AppCard, AppText, AppInput, LoadingSkeleton } from '../../components/ui';
import { useTheme } from '../../theme/use-theme';
import { useCatalogCategories, useCatalogServices, useCatalogService } from './queries';
import { CatalogIcon } from './CatalogIdentity';
import { categoryName, categoryPath, visibleCategories as findVisibleCategories } from './tree';
import type { CatalogCategory, CatalogService, CatalogMode, CatalogSelection } from './contracts';
type Row = {
    kind: 'heading';
    id: string;
    text: string;
} | {
    kind: 'category';
    id: string;
    value: CatalogCategory;
} | {
    kind: 'service';
    id: string;
    value: CatalogService;
};
export function CatalogPicker({ mode, onSelect, onClose, backRef }: {
    mode: CatalogMode;
    onSelect(value: CatalogSelection): void;
    onClose(): void;
    backRef?: RefObject<(() => void) | null>;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    const categories = useCatalogCategories();
    const [search, setSearch] = useState('');
    const [debounced, setDebounced] = useState('');
    const [path, setPath] = useState<string[]>([]);
    const [page, setPage] = useState(1);
    useEffect(() => { const timer = setTimeout(() => { setDebounced(search.trim()); setPage(1); }, 300); return () => clearTimeout(timer); }, [search]);
    const currentId = path.at(-1);
    const current = categories.data?.find(x => x.id === currentId);
    const services = useCatalogServices({ applicableTo: mode, countryCode: 'TR', categoryId: debounced ? undefined : currentId, search: debounced || undefined, popular: !currentId && !debounced ? true : undefined, pageNumber: page, pageSize: 20 });
    const visibleCategories = findVisibleCategories(categories.data ?? [], mode, currentId, debounced);
    const back = useCallback(() => {
        if (search) {
            setSearch('');
            setDebounced('');
        }
        else if (path.length) {
            setPath(p => p.slice(0, -1));
            setPage(1);
        }
        else
            onClose();
    }, [search, path.length, onClose]);
    useEffect(() => {
        if (backRef)
            backRef.current = back;
        return () => {
            if (backRef)
                backRef.current = null;
        };
    }, [back, backRef]);
    const categoryRows: Row[] = visibleCategories.length ? [{ kind: 'heading', id: 'categories-heading', text: currentId ? t("catalog:m_9cc94140f48b") : t("catalog:m_e9371667baa4") }, ...visibleCategories.map(value => ({ kind: 'category' as const, id: value.id, value }))] : [];
    const serviceRows: Row[] = services.data?.items.length ? [{ kind: 'heading', id: 'services-heading', text: !currentId && !debounced ? t("catalog:m_1e52995f1656") : t("catalog:m_2ce0a7da99c6") }, ...services.data.items.map(value => ({ kind: 'service' as const, id: value.id, value }))] : [];
    const rows = !currentId && !debounced ? [...serviceRows, ...categoryRows] : [...categoryRows, ...serviceRows];
    const custom = (category: CatalogCategory) => onSelect({ category, service: null });
    const openCategory = (id: string) => { setPath(categoryPath(categories.data ?? [], id).map(c => c.id)); setSearch(''); setDebounced(''); setPage(1); };
    const refresh = () => { void categories.refetch(); void services.refetch(); };
    return <SafeAreaView style={{ flex: 1, direction: isRTL() ? 'rtl' : 'ltr', backgroundColor: colors.background }}><View style={{ paddingHorizontal: 20, paddingTop: 10, gap: 10 }}><AppInput label={t("catalog:m_4be65ffd3c4d")} placeholder={t("catalog:m_62c753f4ee0b")} value={search} onChangeText={setSearch} maxLength={100}/><AppButton title={t("catalog:m_40c306d33ace")} variant="ghost" onPress={back}/></View>
    <FlatList data={rows} keyExtractor={row => `${row.kind}-${row.id}`} keyboardShouldPersistTaps="handled" contentContainerStyle={{ paddingHorizontal: 20, paddingBottom: 30, gap: 10 }} initialNumToRender={12} maxToRenderPerBatch={12} windowSize={5} refreshing={!!(categories.isRefetching || services.isRefetching)} onRefresh={refresh} ListHeaderComponent={<View style={{ gap: 10, marginVertical: 12 }}><AppText variant="title" accessibilityRole="header">{(current ? categoryName(current) : undefined) ?? (mode === 'Subscription' ? t("catalog:m_74581d3a753b") : t("catalog:m_a1d2f560a2b9"))}</AppText>{current && <><AppText variant="caption" tone="muted">{t("catalog:m_ce0be00a3a04", { value0: categoryPath(categories.data ?? [], currentId).map(c => categoryName(c)).join(' → ') })}</AppText><AppButton title={t("catalog:m_9669271ce541")} variant="ghost" onPress={() => { setPath([]); setSearch(''); setDebounced(''); setPage(1); }}/><AppButton title={mode === 'Subscription' ? t("catalog:m_6b467007b709") : t("catalog:m_9ef05e22a5c5")} variant="secondary" onPress={() => custom(current)}/></>}{(!categories.data || !services.data) && !(categories.isError || services.isError) && <LoadingSkeleton />}{(categories.isError || services.isError) && <AppCard><AppText tone="warning">{t("catalog:m_f08718d380a0")}</AppText><AppButton title={t("catalog:m_bfccb99faf79")} variant="secondary" onPress={refresh}/></AppCard>}<AppText variant="caption" tone="muted">{t("catalog:m_d8416cf1ef26")}</AppText>{current && !visibleCategories.length && !services.isPending && !services.data?.items.length && <AppCard><AppText>{t("catalog:m_2c842aaf0967")}</AppText></AppCard>}</View>} ListEmptyComponent={categories.data && services.data ? <AppCard><AppText tone="muted">{t("catalog:m_174c47177720")}</AppText></AppCard> : null} renderItem={({ item }) => item.kind === 'heading' ? <AppText variant="section" style={{ marginTop: 14 }}>{item.text}</AppText> : item.kind === 'category' ? <Pressable testID={`catalog-card-${item.value.code}`} accessibilityRole="button" accessibilityLabel={categoryName(item.value)} onPress={() => openCategory(item.value.id)} style={({ pressed }) => ({ padding: 18, borderRadius: 20, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, opacity: pressed ? .7 : 1, minHeight: 82 })}><View style={{ flexDirection: 'row', gap: 14, alignItems: 'center' }}><CatalogIcon category={item.value}/><View style={{ flex: 1 }}><AppText variant="label">{categoryName(item.value)}</AppText><AppText variant="caption" tone="muted">{debounced ? categoryPath(categories.data ?? [], item.value.id).map(c => categoryName(c)).join(' → ') : t("catalog:m_dc072fb4b5c8")}</AppText></View></View></Pressable> : <Pressable accessibilityRole="button" accessibilityLabel={item.value.displayName} onPress={() => {
                const selected = categories.data?.find(x => x.id === (currentId && item.value.categoryIds.includes(currentId) ? currentId : item.value.categoryId));
                if (selected)
                    onSelect({ category: selected, service: item.value });
            }} style={({ pressed }) => ({ padding: 18, borderRadius: 20, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, opacity: pressed ? .7 : 1, minHeight: 82 })}><View style={{ flexDirection: 'row', gap: 12, alignItems: 'center' }}><CatalogIcon category={categories.data?.find(x => x.id === item.value.categoryId)} service={item.value}/><View style={{ flex: 1 }}><AppText variant="label">{item.value.displayName}</AppText><AppText variant="caption" tone="muted">{categoryPath(categories.data ?? [], item.value.categoryId).map(c => categoryName(c)).join(' → ')}</AppText></View></View></Pressable>} ListFooterComponent={<View style={{ gap: 10, marginTop: 16 }}>{services.data && services.data.totalPages > 1 && <><AppText>{t("catalog:m_9b49b29dfd07", { value0: page, value1: services.data.totalPages })}</AppText><AppButton title={t("catalog:m_b3f4caeb18ea")} variant="secondary" disabled={page <= 1 || services.isFetching} onPress={() => setPage(p => p - 1)}/><AppButton title={t("catalog:m_71c803f67c3c")} variant="secondary" disabled={page >= services.data.totalPages || services.isFetching} onPress={() => setPage(p => p + 1)}/></>}{!current && categories.data?.find(x => x.code === 'other') && <AppButton title={mode === 'Subscription' ? t("catalog:m_caf667e8034a") : t("catalog:m_33c3c73b3f07")} variant="secondary" onPress={() => custom(categories.data!.find(x => x.code === 'other')!)}/>}<AppButton title={t("catalog:m_c7f4be34c02a")} variant="secondary" onPress={refresh}/><AppButton title={t("catalog:m_be1e5431a411")} variant="ghost" onPress={onClose}/></View>}/>
  </SafeAreaView>;
}
export function CatalogSelectionField({ mode, categoryId, serviceId, legacyCategory, initialOpen = false, disabled = false, error, onSelect }: {
    mode: CatalogMode;
    categoryId?: string | null;
    serviceId?: string | null;
    legacyCategory?: string;
    initialOpen?: boolean;
    disabled?: boolean;
    error?: string;
    onSelect(value: CatalogSelection): void;
}) {
    useLocaleRevision();
    const [open, setOpen] = useState(initialOpen);
    const [revision, setRevision] = useState(0);
    const backRef = useRef<(() => void) | null>(null);
    return <><AppCard><AppText variant="label">{t("catalog:m_df43bddffd68")}</AppText><View style={{ marginVertical: 12 }}><CatalogIdentityPlaceholder categoryId={categoryId} serviceId={serviceId} legacyCategory={legacyCategory}/></View><AppButton title={categoryId ? t("catalog:m_1b585966d2ac") : t("catalog:m_5c04ad033072")} variant="secondary" disabled={disabled} onPress={() => { setRevision(n => n + 1); setOpen(true); }}/>{error && <AppText tone="danger" accessibilityRole="alert">{error}</AppText>}</AppCard><Modal testID="catalog-picker-modal" visible={open} animationType="slide" presentationStyle="fullScreen" onRequestClose={() => {
            if (backRef.current)
                backRef.current();
            else
                setOpen(false);
        }}>{open && <CatalogPicker key={revision} backRef={backRef} mode={mode} onClose={() => setOpen(false)} onSelect={value => { onSelect(value); setOpen(false); }}/>}</Modal></>;
}
function CatalogIdentityPlaceholder({ categoryId, serviceId, legacyCategory }: {
    categoryId?: string | null;
    serviceId?: string | null;
    legacyCategory?: string;
}) {
    useLocaleRevision();
    const categories = useCatalogCategories();
    const service = useCatalogService(serviceId);
    const category = categories.data?.find(c => c.id === categoryId);
    return <View style={{ flexDirection: 'row', gap: 12, alignItems: 'center' }}><CatalogIcon key={`${serviceId}-${categoryId}`} category={category} service={serviceId ? service.data : undefined}/><View style={{ flex: 1 }}>{serviceId && service.data && <AppText variant="label">{t("catalog:m_5ad326037b81", { value0: service.data.displayName })}</AppText>}<AppText tone="muted">{category ? categoryPath(categories.data ?? [], category.id).map(c => categoryName(c)).join(' → ') : legacyCategory ?? t("catalog:m_64527440a4b7")}{serviceId ? '' : t("catalog:m_ad6823727591")}</AppText></View></View>;
}
