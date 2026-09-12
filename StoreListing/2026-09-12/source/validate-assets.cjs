const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require(process.env.STORE_SHARP_PATH || 'sharp');
const root = path.resolve(__dirname, '..');
const walk = dir => fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]);
async function main() {
  const manifest = { date: '2026-09-12', game: { ko: '우주 정거장 보급소', en: 'Space Station Supply', package: 'com.secondwindgames.spacestation' }, images: [], copy: [], result: 'PASS' };
  for (const file of walk(path.join(root, 'images'))) {
    const relative = path.relative(root, file).replaceAll('\\', '/');
    const data = fs.readFileSync(file), m = await sharp(data).metadata();
    const icon = relative.includes('app-icon'), feature = relative.includes('feature-graphic');
    const width = icon ? 512 : feature ? 1024 : 1080, height = icon ? 512 : feature ? 500 : 1920;
    if (m.width !== width || m.height !== height || m.format !== 'png' || m.channels !== (icon ? 4 : 3)) throw Error(`Invalid image format: ${relative}`);
    if (icon && data.length > 1024 * 1024) throw Error('Icon exceeds 1MB');
    if (!icon && data.length > 8 * 1024 * 1024) throw Error('Image exceeds 8MB');
    manifest.images.push({ file: relative, width, height, channels: m.channels, bytes: data.length, sha256: crypto.createHash('sha256').update(data).digest('hex'), source: icon ? 'Existing game SVG icon, square export' : feature ? 'Built-in image_gen artwork, mechanical size export' : 'Native Unity UI Toolkit capture; Korean runtime UI; no synthetic UI' });
  }
  if (manifest.images.length !== 7) throw Error('Expected 7 final store images');
  for (const locale of ['ko-KR', 'en-US']) for (const [name, max] of [['title',30], ['short-description',80], ['full-description',4000]]) {
    const relative = `copy/${locale}/${name}.txt`, value = fs.readFileSync(path.join(root, relative), 'utf8').trim();
    const count = [...value].length;
    if (count > max || !count) throw Error(`Invalid description length ${relative}: ${count}`);
    manifest.copy.push({ file: relative, characters: count, maximum: max, text: value });
  }
  fs.writeFileSync(path.join(root, 'manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
  console.log(JSON.stringify({ result: manifest.result, images: manifest.images.map(x => ({ file: x.file, size: `${x.width}x${x.height}`, channels: x.channels, bytes: x.bytes })), copy: manifest.copy.map(x => ({ file: x.file, characters: x.characters, maximum: x.maximum })) }, null, 2));
}
main().catch(e => { console.error(e); process.exit(1); });
