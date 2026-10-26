const fs = require('node:fs'); const path = require('node:path');
const root = path.resolve(__dirname, '..'); const base = path.join(root, 'src/i18n');
const manifest = JSON.parse(fs.readFileSync(path.join(base, 'source-manifest.json'), 'utf8'));
const extra = JSON.parse(fs.readFileSync(path.join(base, 'extra-source.json'), 'utf8'));
const plural = JSON.parse(fs.readFileSync(path.join(base, 'plural-roots.json'), 'utf8'));
const languages = ['tr', 'en', 'de', 'fr', 'es', 'it', 'pt-BR', 'nl', 'ru', 'ar', 'zh-Hans', 'ja', 'ko', 'hi'];
const namespaces = ['common', 'auth', 'dashboard', 'payments', 'subscriptions', 'catalog', 'calendar', 'analytics', 'reminders', 'settings', 'premium', 'errors', 'validation'];
const resources = {}; const issues = []; const counts = {};
function decode(text) { return text.replace(/\\n/g, '\n').replace(/\\u0020/g, ' '); }
function placeholders(text) { return [...new Set([...text.matchAll(/{{\s*(\w+)\s*}}/g)].map(m => m[1]))].sort().join(','); }
function parse(file) {
  const result = new Map(); if (!fs.existsSync(file)) return result;
  for (const line of fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '').split(/\r?\n/)) {
    if (!line.trim() || line.startsWith('#')) continue; const separator = line.indexOf('|'); if (separator < 1) throw new Error('Invalid translation row: ' + path.basename(file));
    const key = line.slice(0, separator); if (result.has(key)) throw new Error('Duplicate translation key: ' + path.basename(file) + ':' + key);
    result.set(key, decode(line.slice(separator + 1)));
  } return result;
}
for (const language of languages) {
  const data = Object.fromEntries(namespaces.map(n => [n, {}])); const drafts = language === 'tr' ? null : parse(path.join(base, 'drafts', language + '.tsv')); const used = new Set();
  function set(fullKey, source, draftKey, override) {
    const separator = fullKey.indexOf(':'); const namespace = fullKey.slice(0, separator), key = fullKey.slice(separator + 1);
    const value = override ?? (language === 'tr' ? decode(source) : drafts.get(draftKey)); used.add(draftKey);
    if (!value?.trim()) { issues.push(language + ': missing ' + fullKey); return; }
    if (placeholders(value) !== placeholders(source)) issues.push(language + ': placeholders ' + fullKey);
    (data[namespace] ??= {})[key] = value;
  }
  const categories = new Intl.PluralRules(language).resolvedOptions().pluralCategories;
  for (const row of manifest) {
    const suffix = row.key.split(':')[1]; set(row.key, row.text, suffix);
    if (plural.includes(suffix)) for (const category of categories) set(row.key + '_' + category, row.text, suffix + '_' + category);
  }
  for (const [key, source] of Object.entries(extra)) {
    if (key.startsWith('common:paymentCount_')) continue;
    set(key, source, key);
  }
  for (const category of categories) set('common:paymentCount_' + category, extra['common:paymentCount_other'], 'common:paymentCount_' + category);
  if (drafts) for (const key of drafts.keys()) if (!used.has(key) && !/^common:paymentCount_(one|other)$/.test(key)) issues.push(language + ': unused draft ' + key);
  resources[language] = data; counts[language] = Object.values(data).reduce((n, rows) => n + Object.keys(rows).length, 0);
  const directory = path.join(base, 'locales', language); fs.mkdirSync(directory, { recursive: true });
  for (const [namespace, values] of Object.entries(data)) fs.writeFileSync(path.join(directory, namespace + '.json'), JSON.stringify(values, null, 2) + '\n');
}
fs.writeFileSync(path.join(base, 'resources.json'), JSON.stringify(resources, null, 2) + '\n');
const serviceNames = Object.fromEntries(Object.entries(resources).map(([language, data]) => [language, Object.fromEntries(Object.entries(data.catalog).filter(([key]) => key.startsWith('serviceTemplate.')).map(([key, value]) => [key.slice('serviceTemplate.'.length), value]))]));
fs.writeFileSync(path.resolve(root, '../src/PayAhead.Infrastructure/Localization/ServiceNames.json'), JSON.stringify(serviceNames, null, 2) + '\n');
const report = { counts, issueCount: issues.length, issues }; fs.writeFileSync(path.join(base, 'translation-audit.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ counts, issueCount: issues.length, firstIssues: issues.slice(0, 8) }));
if (issues.length && !process.argv.includes('--allow-incomplete')) process.exitCode = 1;
