// Service worker publikované aplikace (ui-pattern.md §6): cachuje shell aplikace, aby PWA nastartovala bez sítě.
// Data (API, přihlášení) se nikdy necachují – bez sítě jsou nedostupná a nové poznámky jdou do fronty (UI-004).
self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'cruma-shell-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.webmanifest$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];
const neverCache = [/^\/api\//, /^\/auth\//];

async function onInstall() {
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate() {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const url = new URL(event.request.url);
    if (event.request.method !== 'GET' || neverCache.some(pattern => pattern.test(url.pathname))) {
        return fetch(event.request);
    }

    // Navigace v rámci aplikace dostane index.html z cache (start bez sítě).
    const shouldServeIndexHtml = event.request.mode === 'navigate';
    const request = shouldServeIndexHtml ? 'index.html' : event.request;
    const cache = await caches.open(cacheName);
    const cached = await cache.match(request);
    return cached || fetch(event.request);
}
