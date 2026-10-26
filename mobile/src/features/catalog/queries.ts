import { t, useLocaleRevision } from "../../i18n/runtime";
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { apiClient, session } from '../../services/auth-runtime';
import { useSessionStore } from '../../store/session';
import { ApiError } from '../../services/api-error';
import { categorySchema, serviceSchema, servicePageSchema, type CatalogFilter } from './contracts';
function parse<T>(schema: z.ZodType<T>, data: unknown) { const result = schema.safeParse(data); if (!result.success)
    throw new ApiError(t("catalog:m_df0c13e7b694")); return result.data; }
function useEnabled() { return useSessionStore(s => s.status === 'authenticated'); }
const options = { staleTime: 5 * 60 * 1000, gcTime: 24 * 60 * 60 * 1000, retry: false };
export function useCatalogCategories() { const enabled = useEnabled(); const locale = useLocaleRevision(); return useQuery({ queryKey: ['catalog', 'categories', locale], queryFn: async ({ signal }) => parse(z.array(categorySchema), (await apiClient.get('/api/catalog/categories', { signal, params: { locale } })).data), enabled, ...options }); }
export function useCatalogServices(filter: CatalogFilter, active = true) { const enabled = useEnabled(); const locale = useLocaleRevision(); return useQuery({ queryKey: ['catalog', 'services', locale, filter], queryFn: async ({ signal }) => parse(servicePageSchema, (await apiClient.get('/api/catalog/services', { signal, params: { ...filter, locale } })).data), enabled: enabled && active, ...options }); }
export function useCatalogService(id?: string | null) { const enabled = useEnabled(); const locale = useLocaleRevision(); const epoch = session.credential()?.epoch; return useQuery({ queryKey: ['catalog', 'service', locale, id], queryFn: async ({ signal }) => parse(serviceSchema, (await apiClient.get(`/api/catalog/services/${id}`, { signal, params: { locale }, ...{ sessionEpoch: epoch } })).data), enabled: enabled && !!id, ...options }); }
