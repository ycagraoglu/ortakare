import { access, readFile, readdir, stat } from "node:fs/promises";
import path from "node:path";

const root = process.cwd();
const failures = [];
const warnings = [];

async function exists(relativePath) {
  try {
    await access(path.join(root, relativePath));
    return true;
  } catch {
    return false;
  }
}

function fail(message) {
  failures.push(message);
}

function warn(message) {
  warnings.push(message);
}

if (!(await exists("package-lock.json"))) {
  fail("package-lock.json bulunamadı. Tekrarlanabilir release için npm install çalıştırılıp lockfile commit edilmelidir.");
}

if (!(await exists("dist/index.html"))) {
  fail("dist/index.html bulunamadı. Önce npm run build çalıştırılmalıdır.");
}

if (!(await exists("dist/web.config"))) {
  fail("dist/web.config bulunamadı. IIS SPA fallback ve güvenlik başlıkları release çıktısına kopyalanmamış.");
}

if (await exists("dist/web.config")) {
  const webConfig = await readFile(path.join(root, "dist/web.config"), "utf8");
  const requiredFragments = [
    "SPA fallback",
    "Content-Security-Policy",
    "X-Content-Type-Options",
    "Referrer-Policy",
  ];

  for (const fragment of requiredFragments) {
    if (!webConfig.includes(fragment)) {
      fail(`dist/web.config zorunlu yapılandırmayı içermiyor: ${fragment}`);
    }
  }
}

if (await exists("dist/sw.js")) {
  const serviceWorker = await readFile(path.join(root, "dist/sw.js"), "utf8");
  if (!serviceWorker.includes("CACHE_VERSION")) fail("Service worker cache sürümü bulunamadı.");
  if (!serviceWorker.includes("/api/")) fail("Service worker API cache dışı bırakma kuralı bulunamadı.");
} else {
  fail("dist/sw.js bulunamadı.");
}

if (await exists("dist/assets")) {
  const assets = await readdir(path.join(root, "dist/assets"));
  if (!assets.some((file) => /\.[a-f0-9]{8,}\.(js|css)$/i.test(file))) {
    warn("Hash içeren asset adı tespit edilemedi. Vite çıktı adlandırması manuel kontrol edilmelidir.");
  }

  for (const file of assets) {
    const filePath = path.join(root, "dist/assets", file);
    const info = await stat(filePath);
    if (info.isFile() && info.size === 0) fail(`Boş asset dosyası bulundu: ${file}`);
  }
}

const apiUrl = process.env.VITE_API_URL;
if (!apiUrl) {
  warn("VITE_API_URL process environment içinde bulunamadı; build-time production değeri ayrıca doğrulanmalıdır.");
} else {
  try {
    const url = new URL(apiUrl);
    if (url.protocol !== "https:") fail("Production VITE_API_URL HTTPS kullanmalıdır.");
  } catch {
    fail("VITE_API_URL geçerli bir URL değil.");
  }
}

const release = process.env.VITE_RELEASE;
if (!release || release === "local-development") {
  warn("VITE_RELEASE gerçek commit SHA/build numarası olarak ayarlanmamış.");
}

for (const message of warnings) console.warn(`WARN: ${message}`);
for (const message of failures) console.error(`FAIL: ${message}`);

if (failures.length > 0) {
  console.error(`Release readiness başarısız: ${failures.length} kritik sorun.`);
  process.exit(1);
}

console.log("Release readiness kontrolleri başarılı.");