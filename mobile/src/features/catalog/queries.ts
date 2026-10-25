import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { apiClient, session } from '../../services/auth-runtime';
import { useSessionStore } from '../../store/session';
import { ApiError } from '../../services/api-error';
import { categorySchema, serviceSchema, servicePageSchema, type CatalogFilter } from './contracts';
function parse<T>(schema: z.ZodType<T>, data: unknown) { const result = schema.safeParse(data); if (!result.success) throw new ApiError('Katalog yanıtı geçerli değil. Mevcut kayıtların korunuyor.'); return result.data; }
function useEnabled() { return useSessionStore(s => s.status === 'authenticated'); }
const options = { staleTime: 5 * 60 * 1000, gcTime: 24 * 60 * 60 * 1000, retry: false };
export function useCatalogCategories() { const enabled = useEnabled(); return useQuery({ queryKey: ['catalog', 'categories', 2], queryFn: async ({ signal }) => parse(z.array(categorySchema), (await apiClient.get('/api/catalog/categories', { signal })).data), enabled, ...options }); }
export function useCatalogServices(filter: CatalogFilter, active = true) { const enabled = useEnabled(); return useQuery({ queryKey: ['catalog', 'services', 2, filter], queryFn: async ({ signal }) => parse(servicePageSchema, (await apiClient.get('/api/catalog/services', { signal, params: filter })).data), enabled: enabled && active, ...options }); }
export function useCatalogService(id?: string | null) { const enabled = useEnabled(); const epoch = session.credential()?.epoch; return useQuery({ queryKey: ['catalog', 'service', id], queryFn: async ({ signal }) => parse(serviceSchema, (await apiClient.get(`/api/catalog/services/${id}`, { signal, ...{ sessionEpoch: epoch } })).data), enabled: enabled && !!id, ...options }); }
