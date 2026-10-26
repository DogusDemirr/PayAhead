// One-time reviewed source migration. Run with --write only after inspecting the manifest.
// Does not send text or user data to any external service.
const ts = require('typescript');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '..');
const entries = new Map(); const files = [];
const uiProps = new Set(['title', 'subtitle', 'label', 'message', 'placeholder', 'accessibilityLabel', 'accessibilityHint', 'text', 'actionLabel', 'emptyTitle', 'emptyMessage']);
const brands = new Set(['PayAhead', 'PayAhead Premium', 'Netflix', 'Spotify', 'YouTube Music', 'Disney+', 'ChatGPT', 'Claude', 'Shell', 'Opet', 'Türk Telekom', 'Turkcell', 'Vodafone', 'Android', 'iOS', 'Premium', 'Free', 'TRY', 'USD', 'EUR', 'IANA', 'UTC', 'Expo', 'Expo Go']);
function namespace(file) { const p = file.replaceAll('\\', '/'); for (const [part, ns] of [['/catalog/', 'catalog'], ['/subscriptions/', 'subscriptions'], ['/payments/', 'payments'], ['/occurrences/', 'calendar'], ['/analytics/', 'analytics'], ['/notifications/', 'reminders'], ['/auth', 'auth'], ['AuthScreen', 'auth'], ['ForgotPassword', 'auth'], ['ResetPassword', 'auth'], ['SettingsScreen', 'settings'], ['DashboardScreen', 'dashboard'], ['CalendarScreen', 'calendar'], ['api-error', 'errors']]) if (p.includes(part)) return ns; return 'common'; }
function put(file, text, params = []) {
  const ns = /validation|contracts/.test(file) ? 'validation' : namespace(file);
  const key = ns + ':m_' + crypto.createHash('sha256').update(text).digest('hex').slice(0, 12);
  if (!entries.has(key)) entries.set(key, { key, text, files: [], placeholders: params });
  const entry = entries.get(key); if (!entry.files.includes(file)) entry.files.push(file); return key;
}
function textCandidate(n, f) {
  if (!ts.isStringLiteralLike(n) || !n.text.trim() || brands.has(n.text.trim()) || n.text.trim().length < 2) return false;
  if (ts.isImportDeclaration(n.parent) || ts.isExportDeclaration(n.parent) || ts.isLiteralTypeNode(n.parent) || ts.isPropertyAssignment(n.parent) && n.parent.name === n) return false;
  if (ts.isJsxAttribute(n.parent)) return uiProps.has(n.parent.name.getText(f));
  if (ts.isCallExpression(n.parent) && n.parent.expression.getText(f).endsWith('t')) return false;
  if (/^(https?:|\/|#|rgba?\(|payahead\.)/.test(n.text) || /[{}<>\\]/.test(n.text)) return false;
  return /[çğıöşüÇĞİÖŞÜ]/.test(n.text) || /[a-zA-Z] [a-zA-Z]/.test(n.text) && !/^(Bearer |Content-|application\/)/.test(n.text);
}
function processFile(file, write) {
  const source = fs.readFileSync(file, 'utf8'); const f = ts.createSourceFile(file, source, ts.ScriptTarget.Latest, true, file.endsWith('x') ? ts.ScriptKind.TSX : ts.ScriptKind.TS); let changed = false; let reactive = false;
  const factory = ts.factory; const call = (text, args = []) => { changed = true; const names = args.map((_, i) => 'value' + i); const key = put(path.relative(root, file), text, names); return factory.createCallExpression(factory.createIdentifier('t'), undefined, [factory.createStringLiteral(key), ...(args.length ? [factory.createObjectLiteralExpression(args.map((expression, i) => factory.createPropertyAssignment(names[i], expression)))] : [])]); };
  const transformed = ts.transform(f, [context => {
    const visit = n => {
      if (file.endsWith('.tsx') && ts.isFunctionDeclaration(n) && n.body && (/^[A-Z]/.test(n.name?.text ?? '') || n.modifiers?.some(m => m.kind === ts.SyntaxKind.DefaultKeyword))) {
        reactive = true; const visited = ts.visitEachChild(n, visit, context);
        const hook = factory.createExpressionStatement(factory.createCallExpression(factory.createIdentifier('useLocaleRevision'), undefined, []));
        return factory.updateFunctionDeclaration(visited, visited.modifiers, visited.asteriskToken, visited.name, visited.typeParameters, visited.parameters, visited.type, factory.updateBlock(visited.body, [hook, ...visited.body.statements]));
      }
      if (file.endsWith('.tsx') && ts.isArrowFunction(n) && ts.isVariableDeclaration(n.parent) && ts.isIdentifier(n.parent.name) && /^[A-Z]/.test(n.parent.name.text)) {
        reactive = true; const visited = ts.visitEachChild(n, visit, context); const hook = factory.createExpressionStatement(factory.createCallExpression(factory.createIdentifier('useLocaleRevision'), undefined, []));
        return factory.updateArrowFunction(visited, visited.modifiers, visited.typeParameters, visited.parameters, visited.type, visited.equalsGreaterThanToken, factory.createBlock([hook, ...(ts.isBlock(visited.body) ? visited.body.statements : [factory.createReturnStatement(visited.body)])], true));
      }
      // Collapse complete text-bearing JSX blocks into one parameterized translation.
      if (ts.isJsxElement(n) && n.children.length && n.children.every(c => ts.isJsxText(c) || ts.isJsxExpression(c) && c.expression)) {
        const staticText = n.children.filter(ts.isJsxText).map(c => c.text).join('');
        if (/[A-Za-zçğıöşüÇĞİÖŞÜ]/.test(staticText) && !brands.has(staticText.trim())) {
          let text = '', args = []; for (const c of n.children) { if (ts.isJsxText(c)) text += c.text.replace(/\s+/g, ' '); else { text += '{{value' + args.length + '}}'; args.push(ts.visitNode(c.expression, visit)); } }
          const expression = call(text.trim(), args); return factory.updateJsxElement(n, ts.visitNode(n.openingElement, visit), [factory.createJsxExpression(undefined, expression)], n.closingElement);
        }
      }
      if (ts.isTemplateExpression(n)) {
        const parts = [n.head.text, ...n.templateSpans.map(s => s.literal.text)]; const staticText = parts.join('');
        const attribute = ts.isJsxExpression(n.parent) && ts.isJsxAttribute(n.parent.parent) ? n.parent.parent.name.getText(f) : null;
        if ((/[çğıöşüÇĞİÖŞÜ]/.test(staticText) || attribute && uiProps.has(attribute)) && !(attribute && !uiProps.has(attribute)) && !/^(\/|Bearer |,?\")/.test(staticText)) {
          let text = n.head.text; const args = []; for (const s of n.templateSpans) { text += '{{value' + args.length + '}}' + s.literal.text; args.push(ts.visitNode(s.expression, visit)); } return call(text, args);
        }
      }
      if (textCandidate(n, f)) { const expression = call(n.text); return ts.isJsxAttribute(n.parent) ? factory.createJsxExpression(undefined, expression) : expression; }
      const visited = ts.visitEachChild(n, visit, context);
      if (ts.isPropertyAssignment(visited) && ts.isCallExpression(visited.initializer) && ts.isIdentifier(visited.initializer.expression) && visited.initializer.expression.text === 't') {
        let inside = false; for (let p = n.parent; p; p = p.parent) if (ts.isFunctionLike(p)) { inside = true; break; }
        if (!inside) return factory.createGetAccessorDeclaration(undefined, visited.name, [], undefined, factory.createBlock([factory.createReturnStatement(visited.initializer)], true));
      }
      return visited;
    }; return node => ts.visitNode(node, visit);
  }]);
  if (changed) { files.push(path.relative(root, file)); if (write) {
    const importPath = path.relative(path.dirname(file), path.join(root, 'src/i18n/runtime')).replaceAll('\\', '/'); const moduleName = importPath.startsWith('.') ? importPath : './' + importPath;
    const wanted = ['t', ...(reactive ? ['useLocaleRevision'] : [])]; let existing = false;
    const statements = transformed.transformed[0].statements.map(statement => {
      if (ts.isImportDeclaration(statement) && ts.isStringLiteral(statement.moduleSpecifier) && statement.moduleSpecifier.text === moduleName) {
        existing = true; const bindings = statement.importClause?.namedBindings; const names = ts.isNamedImports(bindings) ? bindings.elements.map(x => x.name.text) : [];
        return factory.updateImportDeclaration(statement, statement.modifiers, factory.createImportClause(false, undefined, factory.createNamedImports([...new Set([...names, ...wanted])].map(name => factory.createImportSpecifier(false, undefined, factory.createIdentifier(name))))), statement.moduleSpecifier, statement.attributes);
      } return statement;
    });
    if (!existing) statements.unshift(factory.createImportDeclaration(undefined, factory.createImportClause(false, undefined, factory.createNamedImports(wanted.map(name => factory.createImportSpecifier(false, undefined, factory.createIdentifier(name))))), factory.createStringLiteral(moduleName)));
    fs.writeFileSync(file, ts.createPrinter({ newLine: ts.NewLineKind.LineFeed }).printFile(factory.updateSourceFile(transformed.transformed[0], statements)));
  } }
  transformed.dispose();
}
function walk(dir) { for (const entry of fs.readdirSync(dir, { withFileTypes: true })) { const file = path.join(dir, entry.name); if (entry.isDirectory() && entry.name !== 'i18n') walk(file); else if (/\.tsx?$/.test(file) && !/brand-assets|languages/.test(file)) processFile(file, process.argv.includes('--write')); } }
walk(path.join(root, 'src'));
fs.mkdirSync(path.join(root, 'src/i18n'), { recursive: true });
if (process.argv.includes('--write')) fs.writeFileSync(path.join(root, 'src/i18n/source-manifest.json'), JSON.stringify([...entries.values()], null, 2) + '\n');
console.log(JSON.stringify({ keys: entries.size, files: files.length, write: process.argv.includes('--write') }));
