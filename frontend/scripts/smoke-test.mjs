const baseUrl = process.env.SMOKE_BASE_URL;

if (!baseUrl) {
  console.error("SMOKE_BASE_URL zorunludur. Örnek: SMOKE_BASE_URL=https://app.example.com npm run smoke");
  process.exit(1);
}

let origin;
try {
  origin = new URL(baseUrl).origin;
} catch {
  console.error("SMOKE_BASE_URL geçerli bir URL değil.");
  process.exit(1);
}

const checks = [
  { name: "Root document", path: "/", contentType: "text/html" },
  { name: "SPA fallback", path: "/release-smoke/non-existing-route", contentType: "text/html" },
  { name: "Manifest", path: "/manifest.webmanifest", contentType: "application/manifest+json" },
  { name: "Service worker", path: "/sw.js", contentType: "javascript" },
];

let failed = 0;

for (const check of checks) {
  const url = new URL(check.path, origin);
  try {
    const response = await fetch(url, {
      redirect: "error",
      headers: { "Cache-Control": "no-cache" },
    });

    const contentType = response.headers.get("content-type") ?? "";
    const ok = response.ok && contentType.toLowerCase().includes(check.contentType);

    if (!ok) {
      failed += 1;
      console.error(`FAIL: ${check.name} — HTTP ${response.status}, content-type=${contentType || "missing"}`);
      continue;
    }

    console.log(`PASS: ${check.name} — HTTP ${response.status}`);
  } catch (error) {
    failed += 1;
    console.error(`FAIL: ${check.name} — ${error instanceof Error ? error.message : "unknown error"}`);
  }
}

try {
  const response = await fetch(origin, { method: "HEAD", redirect: "error" });
  const requiredHeaders = [
    "content-security-policy",
    "referrer-policy",
    "x-content-type-options",
    "x-frame-options",
  ];

  for (const header of requiredHeaders) {
    if (!response.headers.has(header)) {
      failed += 1;
      console.error(`FAIL: Güvenlik başlığı eksik: ${header}`);
    } else {
      console.log(`PASS: Güvenlik başlığı mevcut: ${header}`);
    }
  }
} catch (error) {
  failed += 1;
  console.error(`FAIL: Güvenlik başlığı kontrolü — ${error instanceof Error ? error.message : "unknown error"}`);
}

if (failed > 0) {
  console.error(`Smoke test başarısız: ${failed} kontrol geçmedi.`);
  process.exit(1);
}

console.log("Production smoke test başarılı.");