import { test, expect } from '@jest/globals';
import { categorySchema } from '../src/features/catalog/contracts';
import { accessibleCategories, categoryPath, visibleCategories } from '../src/features/catalog/tree';
import fixture from './catalog-tree.fixture.json';
const categories = fixture.map(c => categorySchema.parse(c));

test('all active categories are reachable through their actual ancestors in each allowed mode', () => {
  expect(categories).toHaveLength(116);
  for (const mode of ['Payment', 'Subscription'] as const) {
    const all = accessibleCategories(categories, mode);
    for (const category of all) {
      const path = categoryPath(categories, category.id);
      expect(visibleCategories(categories, mode).some(c => c.id === path[0].id)).toBe(true);
      for (let i = 1; i < path.length; i++) expect(visibleCategories(categories, mode, path[i - 1].id).some(c => c.id === path[i].id)).toBe(true);
    }
    expect(all.every(c => categoryPath(categories, c.id).every(parent => parent.isActive))).toBe(true);
  }
  const vehicles = categories.filter(c => c.code.startsWith('transport.'));
  expect(vehicles.every(c => accessibleCategories(categories, 'Payment').includes(c))).toBe(true);
  expect(vehicles.some(c => accessibleCategories(categories, 'Subscription').includes(c))).toBe(false);
  expect(categories.filter(c => c.code.startsWith('news.'))).toHaveLength(10);
});
test('search traverses the full Turkish hierarchy and digital shortcuts preserve full breadcrumb', () => {
  expect(visibleCategories(categories, 'Payment', undefined, 'ARAÇ').some(c => c.code === 'transport.petrol')).toBe(true);
  expect(visibleCategories(categories, 'Subscription', undefined, 'yayincilik').filter(c => c.code.startsWith('news.'))).toHaveLength(10);
  const video = categories.find(c => c.code === 'video')!;
  expect(visibleCategories(categories, 'Subscription').some(c => c.id === video.id)).toBe(true);
  expect(categoryPath(categories, video.id).map(c => c.code)).toEqual(['digital', 'video']);
});
