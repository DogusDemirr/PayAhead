const ts = require('typescript'); const fs = require('node:fs'); const path = require('node:path'); const crypto = require('node:crypto');
const root = path.resolve(__dirname, '..'); const manifestFile = path.join(root, 'src/i18n/source-manifest.json'); const manifest = JSON.parse(fs.readFileSync(manifestFile, 'utf8'));
const words = new Set(['Takvim', 'Abonelikler', 'Ayarlar', 'Bilgi', 'Aktif', 'Bekliyor', 'Pzt', 'Sal', 'Per', 'Cum', 'Cmt', 'Paz', 'Gecikti', 'Hizmet', 'Kategoriler', 'Hizmetler', 'Planlar', 'Sil', 'Belirtilmedi', 'Tutar', 'Kategori', 'Pasif', 'Koyu', 'Sistem', 'Yeni abonelik']);
const factory = ts.factory;
function ns(file) { return file.includes('catalog') ? 'catalog' : file.includes('subscription') || file.includes('Subscription') ? 'subscriptions' : file.includes('payment') || file.includes('Payment') ? 'payments' : file.includes('Calendar') || file.includes('occurrence') ? 'calendar' : file.includes('Settings') ? 'settings' : 'common'; }
function walk(dir) { for (const entry of fs.readdirSync(dir, { withFileTypes: true })) { const file = path.join(dir, entry.name); if (entry.isDirectory() && entry.name !== 'i18n') walk(file); else if (/\.tsx?$/.test(file)) {
  const f = ts.createSourceFile(file, fs.readFileSync(file, 'utf8'), ts.ScriptTarget.Latest, true, file.endsWith('x') ? ts.ScriptKind.TSX : ts.ScriptKind.TS); let changed = false;
  const result = ts.transform(f, [context => { const visit = n => {
    if (ts.isStringLiteralLike(n) && words.has(n.text)) {
      const key = ns(file) + ':m_' + crypto.createHash('sha256').update(n.text).digest('hex').slice(0, 12); const relative = path.relative(root, file); let row = manifest.find(r => r.key === key); if (!row) { row = { key, text: n.text, placeholders: [], files: [] }; manifest.push(row); } if (!row.files.includes(relative)) row.files.push(relative);
      changed = true; const expression = factory.createCallExpression(factory.createIdentifier('t'), undefined, [factory.createStringLiteral(key)]); return ts.isJsxAttribute(n.parent) ? factory.createJsxExpression(undefined, expression) : expression;
    }
    return ts.visitEachChild(n, visit, context);
  }; return node => ts.visitNode(node, visit); }]);
  if (changed) fs.writeFileSync(file, ts.createPrinter({ newLine: ts.NewLineKind.LineFeed }).printFile(result.transformed[0])); result.dispose();
} } }
walk(path.join(root, 'src')); fs.writeFileSync(manifestFile, JSON.stringify(manifest, null, 2) + '\n'); console.log('Translation keys:', manifest.length);
