import { normalizeCatalogSearch, type CatalogCategory, type CatalogMode } from './contracts';

export function categoryPath(categories: CatalogCategory[], id?: string): CatalogCategory[] {
  const path: CatalogCategory[] = []; const seen = new Set<string>(); let current = categories.find(c => c.id === id);
  while (current && !seen.has(current.id)) { seen.add(current.id); path.unshift(current); current = categories.find(c => c.id === current!.parentId); }
  return path;
}
export function accessibleCategories(categories: CatalogCategory[], mode: CatalogMode) {
  return categories.filter(c => categoryPath(categories, c.id).every(p => p.isActive && (p.applicableTo === mode || p.applicableTo === 'Both')));
}
export function visibleCategories(categories: CatalogCategory[], mode: CatalogMode, currentId?: string, search = '') {
  const q = normalizeCatalogSearch(search); const rows = accessibleCategories(categories, mode);
  return rows.filter(c => q ? normalizeCatalogSearch(categoryPath(categories, c.id).map(p => `${p.nameTr} ${p.nameEn} ${p.code}`).join(' ')).includes(q)
    : currentId ? c.parentId === currentId : !c.parentId || categories.some(parent => parent.id === c.parentId && parent.code === 'digital'));
}
