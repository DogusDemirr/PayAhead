const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const directory = path.join(__dirname, '../assets/brand');
fs.mkdirSync(directory, { recursive: true });
// Original vector mark: a forward/upward path inside a calm navy field. No third-party logos.
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024"><rect width="1024" height="1024" fill="#101D35"/><rect x="212" y="212" width="600" height="600" rx="180" fill="#14B8A6"/><path d="M365 659L659 365M395 365H659V629" fill="none" stroke="#101D35" stroke-width="72" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
fs.writeFileSync(path.join(directory, 'mark.svg'), svg);
Promise.all([sharp(Buffer.from(svg)).png().toFile(path.join(directory, 'icon.png')), sharp(Buffer.from(svg)).resize(256).png().toFile(path.join(directory, 'splash.png'))]).catch((error) => { console.error(error.message); process.exitCode = 1; });
