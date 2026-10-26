import { z } from 'zod';
import { frequencySchema, pageSchema } from '../payments/contracts';
const applicability = z.enum(['Payment', 'Subscription', 'Both']);
export const catalogReferences = { catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional() };
export const categorySchema = z.object({ id: z.uuid(), parentId: z.uuid().nullable(), code: z.string(), nameTr: z.string(), nameEn: z.string(), displayName: z.string().optional(), cultureCode: z.string().optional(), usedLanguageFallback: z.boolean().optional(), iconKey: z.string(), sortOrder: z.number().int(), isActive: z.boolean(), applicableTo: applicability });
export const serviceSchema = z.object({ id: z.uuid(), code: z.string(), displayName: z.string(), categoryId: z.uuid(), categoryIds: z.array(z.uuid()), logoKey: z.string().nullable(), searchKeywords: z.string(), sortOrder: z.number().int(), isPopular: z.boolean(), isActive: z.boolean(), applicableTo: applicability, defaultRecurrenceType: frequencySchema.nullable(), countryCode: z.string().nullable() });
export const servicePageSchema = pageSchema(serviceSchema);
export type CatalogCategory = z.infer<typeof categorySchema>;
export type CatalogService = z.infer<typeof serviceSchema>;
export type CatalogMode = 'Payment' | 'Subscription';
export interface CatalogSelection { category: CatalogCategory; service: CatalogService | null }
export interface CatalogFilter { categoryId?: string; applicableTo: CatalogMode; search?: string; popular?: boolean; countryCode?: string; pageNumber: number; pageSize: number }
export function normalizeCatalogSearch(value: string) { return value.replace(/ı/g, 'i').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim(); }
