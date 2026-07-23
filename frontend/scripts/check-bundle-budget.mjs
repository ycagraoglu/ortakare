import { readdir, stat } from "node:fs/promises";
import { extname, join, relative } from "node:path";

const DIST_DIRECTORY = new URL("../dist/", import.meta.url);
const MAX_INITIAL_JS_BYTES = 500 * 1024;
const MAX_SINGLE_JS_BYTES = 350 * 1024;
const MAX_SINGLE_CSS_BYTES = 120 * 1024;

async function collectFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const path = join(directory.pathname, entry.name);
    if (entry.isDirectory()) files.push(...(await collectFiles(new URL(`${entry.name}/`, directory))));
    else files.push(path);
  }

  return files;
}

function formatKilobytes(bytes) {
  return `${(bytes / 1024).toFixed(1)} KB`;
}

try {
  await stat(DIST_DIRECTORY);
} catch {
  console.error("Bundle budget kontrolü için önce npm run build çalıştırılmalıdır.");
  process.exit(1);
}

const files = await collectFiles(DIST_DIRECTORY);
const assets = await Promise.all(
  files.map(async (path) => ({
    path,
    extension: extname(path),
    size: (await stat(path)).size,
  })),
);

const javascriptAssets = assets.filter((asset) => asset.extension === ".js");
const cssAssets = assets.filter((asset) => asset.extension === ".css");
const failures = [];

const totalJavascriptBytes = javascriptAssets.reduce((total, asset) => total + asset.size, 0);
if (totalJavascriptBytes > MAX_INITIAL_JS_BYTES) {
  failures.push(
    `Toplam JavaScript ${formatKilobytes(totalJavascriptBytes)}; limit ${formatKilobytes(MAX_INITIAL_JS_BYTES)}.`,
  );
}

for (const asset of javascriptAssets) {
  if (asset.size > MAX_SINGLE_JS_BYTES) {
    failures.push(
      `${relative(DIST_DIRECTORY.pathname, asset.path)} ${formatKilobytes(asset.size)}; tek JS limit ${formatKilobytes(MAX_SINGLE_JS_BYTES)}.`,
    );
  }
}

for (const asset of cssAssets) {
  if (asset.size > MAX_SINGLE_CSS_BYTES) {
    failures.push(
      `${relative(DIST_DIRECTORY.pathname, asset.path)} ${formatKilobytes(asset.size)}; tek CSS limit ${formatKilobytes(MAX_SINGLE_CSS_BYTES)}.`,
    );
  }
}

console.table(
  assets
    .filter((asset) => [".js", ".css"].includes(asset.extension))
    .sort((left, right) => right.size - left.size)
    .map((asset) => ({
      asset: relative(DIST_DIRECTORY.pathname, asset.path),
      size: formatKilobytes(asset.size),
    })),
);

if (failures.length > 0) {
  console.error("Bundle budget aşıldı:\n- " + failures.join("\n- "));
  process.exit(1);
}

console.log("Bundle budget kontrolü başarılı.");
