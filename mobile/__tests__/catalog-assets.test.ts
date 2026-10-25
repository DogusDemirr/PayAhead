import { test, expect } from '@jest/globals';
import fs from 'node:fs';
import path from 'node:path';
import { createHash } from 'node:crypto';
import manifest from '../assets/brands/manifest.json';
import { localServiceLogos } from '../src/features/catalog/brand-assets';

test('all five real local logos have valid originals, PNG signatures, immutable mappings and source evidence', () => {
  expect(manifest).toHaveLength(5);
  expect(Object.keys(localServiceLogos).sort()).toEqual(manifest.map(x => x.serviceId).sort());
  for (const row of manifest) {
    const png = fs.readFileSync(path.join(__dirname, '..', row.asset));
    expect(png.subarray(0, 8).toString('hex')).toBe('89504e470d0a1a0a');
    expect(png.length).toBeLessThan(30000);
    const original = fs.readFileSync(path.join(__dirname, '..', row.original));
    expect(createHash('sha256').update(original).digest('hex')).toBe(row.sha256);
    expect(original.toString()).toContain('<svg');
    expect(row.copyrightStatus).toBe('PD-textlogo');
    expect(row.source).toMatch(/^https:\/\/commons.wikimedia.org\/wiki\/File:/);
  }
});
