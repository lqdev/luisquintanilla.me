/**
 * One-shot generator for the homepage avatar flip-card QR.
 *
 * Uses Playwright (already a dev dep) to load the EXACT same
 * `qr-code-styling@1.5.0` library + config that powers the per-page QR
 * modal at runtime (see `_src/js/qrcode.js`). The rendered SVG is then
 * extracted from the headless DOM and written to
 * `_src/assets/images/contact/qr-home.svg` so the homepage flip-card
 * gets pixel-parity styling with the per-page modal — without ever
 * loading the QR library on the homepage hot path.
 *
 * Run with:  node Scripts/generate-qr-home-styled.mjs
 *
 * The avatar.png is inlined as a base64 data URI so the resulting SVG is
 * fully self-contained (no broken image refs if served from a different
 * origin, no cross-origin tainting, no second HTTP request).
 */

import { chromium } from '@playwright/test';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const AVATAR_PATH = resolve(repoRoot, '_public', 'avatar.png');
const OUTPUT_PATH = resolve(repoRoot, '_src', 'assets', 'images', 'contact', 'qr-home.svg');
const TARGET_URL = 'https://www.luisquintanilla.me/';

// Inline avatar so the SVG is fully self-contained.
// The avatar renders at 25% of 280 = 70px inside the QR, so 140x140 (2x for
// retina) is plenty. Downscaling keeps the final SVG ~10x smaller than
// embedding the source 400x400 avatar.
const avatarDataUri = await (async () => {
  try {
    const sharp = (await import('sharp')).default;
    const resized = await sharp(AVATAR_PATH)
      .resize(96, 96, { fit: 'cover' })
      .png({ compressionLevel: 9 })
      .toBuffer();
    return `data:image/png;base64,${resized.toString('base64')}`;
  } catch (err) {
    // sharp is optional; fall back to embedding the source avatar as-is.
    if (err && err.code === 'ERR_MODULE_NOT_FOUND') {
      try {
        const bytes = readFileSync(AVATAR_PATH);
        console.warn('sharp not installed; embedding full-size avatar (SVG will be larger).');
        return `data:image/png;base64,${bytes.toString('base64')}`;
      } catch (readErr) {
        console.warn(`Could not read avatar at ${AVATAR_PATH}: ${readErr.message}`);
        return null;
      }
    }
    console.warn(`Could not process avatar at ${AVATAR_PATH}: ${err.message}`);
    return null;
  }
})();

const html = `<!doctype html>
<html><head><meta charset="utf-8"><title>QR gen</title></head>
<body>
<div id="qr"></div>
<script src="https://cdn.jsdelivr.net/npm/qr-code-styling@1.5.0/lib/qr-code-styling.min.js"></script>
<script>
  window.__renderQR = async function(url, avatar) {
    const qr = new QRCodeStyling({
      width: 280,
      height: 280,
      type: 'svg',
      data: url,
      image: avatar || undefined,
      dotsOptions: { color: '#1a2332', type: 'rounded' },
      cornersSquareOptions: { color: '#ff6b35', type: 'extra-rounded' },
      cornersDotOptions: { color: '#ff6b35' },
      backgroundOptions: { color: '#ffffff' },
      imageOptions: { crossOrigin: 'anonymous', margin: 8, imageSize: 0.25 }
    });
    const container = document.getElementById('qr');
    qr.append(container);
    // qr-code-styling loads the image async; give it a tick to draw.
    await new Promise(r => setTimeout(r, 500));
    const svg = container.querySelector('svg');
    return svg ? svg.outerHTML : null;
  };
</script>
</body></html>`;

const browser = await chromium.launch({ headless: true });
try {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await page.setContent(html, { waitUntil: 'networkidle' });

  const svg = await page.evaluate(
    async ([url, avatar]) => await window.__renderQR(url, avatar),
    [TARGET_URL, avatarDataUri]
  );

  if (!svg) {
    throw new Error('qr-code-styling did not produce an <svg> element.');
  }

  // Add an XML prolog so the file is a valid standalone SVG document.
  const out = `<?xml version="1.0" encoding="UTF-8"?>\n${svg}\n`;
  writeFileSync(OUTPUT_PATH, out, 'utf8');
  console.log(`✅ Wrote ${OUTPUT_PATH} (${out.length} bytes)`);
} finally {
  await browser.close();
}
