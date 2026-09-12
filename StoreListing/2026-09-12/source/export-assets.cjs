const fs = require('fs');
const path = require('path');
const sharp = require(process.env.STORE_SHARP_PATH || 'sharp');
const root = path.resolve(__dirname, '..');

async function main() {
  const images = path.join(root, 'images');
  fs.mkdirSync(images, { recursive: true });
  // Existing game icon artwork, exported without a baked-in corner mask.
  await sharp(path.join(__dirname, 'app-icon-square.svg'))
    .resize(512, 512).ensureAlpha(1).png({ compressionLevel: 9 })
    .toFile(path.join(images, 'app-icon-512.png'));
  for (const locale of ['ko-KR', 'en-US']) {
    // Only mechanical store-format export of the finished imagegen master.
    await sharp(path.join(__dirname, `feature-master-${locale}.png`))
      .resize(1024, 500, { fit: 'cover', position: 'centre' })
      .flatten({ background: '#122f41' }).removeAlpha()
      .png({ compressionLevel: 9 })
      .toFile(path.join(images, `feature-graphic-${locale}-1024x500.png`));
  }
  console.log('Exported store icon and two feature graphics.');
}
main().catch(e => { console.error(e); process.exit(1); });
