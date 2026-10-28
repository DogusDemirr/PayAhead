// Deterministic source/resource audit; never rewrites the extraction manifest.
const fs = require('node:fs'); const path = require('node:path'); const ts = require('typescript');
const root = path.resolve(__dirname, '..'); const base = path.join(root, 'src/i18n');
const resources = JSON.parse(fs.readFileSync(path.join(base, 'resources.json'), 'utf8'));
const manifest = JSON.parse(fs.readFileSync(path.join(base, 'source-manifest.json'), 'utf8'));
const extra = JSON.parse(fs.readFileSync(path.join(base, 'extra-source.json'), 'utf8'));
const retired = require('../src/i18n/retired-source.json');
const issues = []; const used = new Set(); const literals = [];
const semanticUsed = new Set();
let allSource = '';
const allowed = new Set(['PayAhead', 'PayAhead Premium', 'Premium', 'Free', 'TRY', 'USD', 'EUR', 'UTC', 'IANA', 'Expo Go', 'Android', 'iOS']);
const props = new Set(['title', 'subtitle', 'label', 'message', 'placeholder', 'accessibilityLabel', 'accessibilityHint', 'action', 'actionLabel', 'emptyTitle', 'emptyMessage']);
function inspect(file) {
  const source = fs.readFileSync(file, 'utf8');
  allSource += source;
  for (const match of source.matchAll(/["']([a-z]+:m_[a-f0-9]+)["']/g)) used.add(match[1]);
  const tree = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true, file.endsWith('x') ? ts.ScriptKind.TSX : ts.ScriptKind.TS);
  function visit(node) {
    if (ts.isCallExpression(node) && node.expression.getText(tree) === 'key') {
      const collect = item => { if (ts.isStringLiteral(item) && extra['settings:profile.' + item.text]) semanticUsed.add('settings:profile.' + item.text); ts.forEachChild(item, collect); };
      node.arguments.forEach(collect);
    }
    if (file.replaceAll('\\', '/').endsWith('/account/AccountScreen.tsx') && ts.isStringLiteral(node) && extra['settings:profile.' + node.text]) semanticUsed.add('settings:profile.' + node.text);
    let text;
    if (ts.isJsxText(node)) text = node.text.trim();
    if (ts.isStringLiteral(node) && ts.isJsxAttribute(node.parent) && props.has(node.parent.name.getText(tree))) text = node.text.trim();
    if (ts.isStringLiteral(node) && /[çğıöşüÇĞİÖŞÜ]/.test(node.text)) text = node.text.trim();
    if (node.parent && ts.isCallExpression(node.parent)) {
      const call = node.parent.expression.getText(tree);
      if (ts.isStringLiteral(node) && ((call === 'input' && node.parent.arguments[1] === node) || (call === 'Alert.alert' && node.parent.arguments.indexOf(node) < 2))) text = node.text.trim();
    }
    if (ts.isTemplateExpression(node) && /[çğıöşüÇĞİÖŞÜ]|Planlanan/.test(node.head.text + node.templateSpans.map(x => x.literal.text).join(''))) text = node.getText(tree);
    if (text && /\p{L}/u.test(text) && !allowed.has(text) && !/^[A-Z]{3}$/.test(text)) literals.push(path.relative(root, file) + ':' + (tree.getLineAndCharacterOfPosition(node.getStart(tree)).line + 1) + ' ' + text);
    ts.forEachChild(node, visit);
  } visit(tree);
}
function walk(dir) { for (const item of fs.readdirSync(dir, { withFileTypes: true })) { const file = path.join(dir, item.name); if (item.isDirectory() && item.name !== 'i18n') walk(file); else if (item.isFile() && /\.tsx?$/.test(file)) inspect(file); } }
walk(path.join(root, 'src'));
for (const row of manifest) if (!used.has(row.key) && !retired[row.key]) issues.push('Unused source key: ' + row.key);
for (const [key, reason] of Object.entries(retired)) if (!reason || !manifest.some(row => row.key === key) || used.has(key)) issues.push('Invalid retired key: ' + key);
for (const key of used) if (!manifest.some(row => row.key === key)) issues.push('Missing manifest key: ' + key);
for (const key of Object.keys(extra)) {
  // Generic template entries are consumed by the embedded backend resource, not mobile t().
  if (key.startsWith('catalog:serviceTemplate.')) continue;
  const rootKey = key.replace(/_(one|other)$/, '');
  if (!allSource.includes(rootKey) && !semanticUsed.has(rootKey)) issues.push('Unused semantic source key: ' + key);
}
for (const [locale, namespaces] of Object.entries(resources)) for (const [namespace, rows] of Object.entries(namespaces)) {
  const file = JSON.parse(fs.readFileSync(path.join(base, 'locales', locale, namespace + '.json'), 'utf8'));
  if (JSON.stringify(file) !== JSON.stringify(rows)) issues.push('Bundle mismatch: ' + locale + '/' + namespace);
}
issues.push(...literals.map(x => 'Raw UI literal: ' + x));
const report = { sourceKeys: manifest.length, usedSourceKeys: used.size, rawLiterals: literals, issues };
fs.writeFileSync(path.join(base, 'source-audit.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2)); if (issues.length) process.exitCode = 1;
