const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const app = require('../app.json').expo;
const id = process.env.EXPO_PUBLIC_EAS_PROJECT_ID;
const configPath = process.env.GOOGLE_SERVICES_JSON;
const failures = [];
if (!id || !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(id)) failures.push('Gerçek EXPO_PUBLIC_EAS_PROJECT_ID gerekli.');
if (!configPath) failures.push('GOOGLE_SERVICES_JSON dosya yolu gerekli.');
else {
  try { const value = JSON.parse(fs.readFileSync(path.resolve(root, configPath), 'utf8')); if (!value.client?.some(client => client.client_info?.android_client_info?.package_name === app.android.package)) failures.push('Firebase Android client package ile uygulama package eşleşmiyor.'); }
  catch { failures.push('Firebase Android yapılandırma dosyası okunamadı.'); }
}
console.log('Android application ID: ' + app.android.package);
if (failures.length) { failures.forEach(x => console.error(x)); process.exitCode = 1; }
else console.log('Yerel projectId/Firebase client eşleşmesi doğrulandı. EAS FCM v1 credential, backend Expo access token ve gerçek cihaz teslimi ayrıca doğrulanmalıdır.');
