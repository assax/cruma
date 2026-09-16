// Vývojová verze service workeru: nic necachuje, aby se změny projevily hned.
// Publikovaná verze (service-worker.published.js) cachuje shell aplikace pro start bez sítě (ui-pattern.md §6).
self.addEventListener('fetch', () => { });
